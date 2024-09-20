using EntityStates;
using RoR2;
using UnityEngine;
using EntityStates.LemurianBruiserMonster;
using EntityStates.Mage;
using UnityEngine.UIElements;
using UnityEngine.Networking;


namespace MageEruption
{
    public class MageEruptionState : GenericCharacterMain
    {
        private Vector3 flyVector = Vector3.zero;
        private Vector3 blastPosition;
        public float baseDuration = 1.2f;
        private float aoe = Mathf.Max(MageEruption.BlastArea.Value, 0f);
        private float dmg = Mathf.Max(MageEruption.BlastDmg.Value, 0f);
        private float proc = Mathf.Max(MageEruption.BlastProc.Value, 0f);
        private float force = Mathf.Max(MageEruption.BlastForce.Value, 0f) * 100f;
        private bool vfxoff = MageEruption.VFXoff.Value;

        public override void OnEnter()
        {
            base.OnEnter();
            Debug.Log("base done");
            float angleV = base.isGrounded ? 1.1f : 3.3f;
            this.flyVector = Vector3.Normalize(base.characterDirection.forward + Vector3.up / angleV);
            Debug.Log(this.flyVector);
            Util.PlaySound(FlyUpState.beginSoundString, base.gameObject);
            Debug.Log(FlyUpState.beginSoundString);
            this.CreateBlinkEffect(base.characterBody.corePosition);
            base.PlayCrossfade("Body", "FlyUp", "FlyUp.playbackRate", this.baseDuration, 0.1f);
            base.characterMotor.Motor.ForceUnground();
            base.characterMotor.velocity = Vector3.zero;
            this.blastPosition = base.characterBody.corePosition;
            bool hasAOE = base.isAuthority && this.aoe > 0f;
            if (hasAOE)
            {
                BlastAttack blastAttack = new BlastAttack
                {
                    radius = this.aoe,
                    procCoefficient = this.proc,
                    position = this.blastPosition,
                    attacker = base.gameObject,
                    crit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master),
                    baseDamage = base.characterBody.damage * this.dmg,
                    falloffModel = 0,
                    baseForce = this.force,
                    attackerFiltering = AttackerFiltering.NeverHitSelf,
                    damageType = DamageType.IgniteOnHit
                };
                blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
                blastAttack.Fire();
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            bool timeUp = base.fixedAge >= this.baseDuration -0.4 && base.isAuthority;
            if (timeUp)
            {
                this.outer.SetNextStateToMain();
            }
        }
        public override void OnExit()
        {
            bool done = !this.outer.destroying;
            if (done)
            {
                Util.PlaySound(FlyUpState.endSoundString, base.gameObject);
            }
            base.OnExit();
        }
        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(this.blastPosition);
        }
        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            this.blastPosition = reader.ReadVector3();
        }
        public override void HandleMovements()
        {
            base.HandleMovements();
            float finalSpeed = 2f + this.moveSpeedStat * (base.characterBody.isSprinting ? (1f / base.characterBody.sprintingSpeedMultiplier) : 1f) / 2f * FlyUpState.speedCoefficientCurve.Evaluate(base.fixedAge / this.baseDuration);
            base.characterMotor.rootMotion += this.flyVector * finalSpeed * Time.fixedDeltaTime;
            CharacterMotor characterMotor = base.characterMotor;
            characterMotor.velocity.y = characterMotor.velocity.y + 0.3f;
        }
        //public override void ProcessJump()
        //{
        //    base.ProcessJump();
        //    this.jetpackStateMachine.SetNextState(new Idle());
        //}
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
        private void CreateBlinkEffect(Vector3 origin)
        {
            if (!vfxoff)
            {
                EffectData effectData = new EffectData
                {
                    rotation = Util.QuaternionSafeLookRotation(this.flyVector),
                    origin = origin,
                    scale = this.aoe
                };
                EffectManager.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/ImpactEffects/ExplosionSolarFlare"), effectData, false);
                EffectManager.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/ImpactEffects/MagmaOrbExplosion"), effectData, false);
            }
            EffectManager.SimpleMuzzleFlash(FireMegaFireball.muzzleflashEffectPrefab, base.gameObject, "MuzzleLeft", false);
            EffectManager.SimpleMuzzleFlash(FireMegaFireball.muzzleflashEffectPrefab, base.gameObject, "MuzzleRight", false);
        }
    }
}
