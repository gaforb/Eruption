using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.Skills;
using EntityStates;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MageEruption
{
    [BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class MageEruption : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "gaforb";
        public const string PluginName = "MageEruption";
        public const string PluginVersion = "3.2.0";

        public static ConfigEntry<bool> VFXoff { get; set; }

        public static ConfigEntry<bool> IsSprint { get; set; }
        public static ConfigEntry<bool> IsSpecial { get; set; }

        public static ConfigEntry<float> BlastDmg { get; set; }
        public static ConfigEntry<float> BlastArea { get; set; }
        public static ConfigEntry<float> BlastProc { get; set; }
        public static ConfigEntry<float> BlastForce { get; set; }
        public static ConfigEntry<float> Cooldown { get; set; }
        public static ConfigEntry<int> Stocks { get; set; }

        public void Awake()
        {
            MageEruption.VFXoff = base.Config.Bind<bool>("General", "Use simple explosion", false, "Disables some graphical effects, improving performance.");
            MageEruption.IsSprint = base.Config.Bind<bool>("General", "Force sprinting", false, "Makes Eruption work like other utility skills and force sprint. if True, using Flamethrower during Eruption will waste the Flamethrower stock.");
            MageEruption.IsSpecial = base.Config.Bind<bool>("General", "Move to special", false, "Adds Eruption to the Special slot instead of the Utility slot.");
            MageEruption.BlastDmg = base.Config.Bind<float>("Values", "Blast damage", 3.6f, "How much damage the initial blast deals. 1 = 100%. Setting to 0 will still apply burn.");
            MageEruption.BlastArea = base.Config.Bind<float>("Values", "Blast area", 8f, "How much range the intial blast has. Set to 0 to disable blast attack completely");
            MageEruption.BlastProc = base.Config.Bind<float>("Values", "Blast proc coefficient", 1f, "How well Eruption triggers item effects. Also effects burn duration.");
            MageEruption.BlastForce = base.Config.Bind<float>("Values", "Blast force", 12f, "How much Eruption knocks back enemies.");
            MageEruption.Cooldown = base.Config.Bind<float>("Values", "Cooldown", 7f, "How long Eruption takes to recharge.");
            MageEruption.Stocks = base.Config.Bind<int>("Values", "Stocks", 1, "Number of stocks Eruption has.");

            LanguageAPI.Add("MAGE_ERUPTION_FIRE", "Eruption");
            LanguageAPI.Add("MAGE_ERUPTION_DESCRIPTION", "Blast forward" + ((MageEruption.BlastArea.Value > 0f) ? (", " + ((MageEruption.BlastDmg.Value > 0f) ? string.Format("dealing <style=cIsDamage>{0}%</style> damage and ", MageEruption.BlastDmg.Value * 100f) : "") + "<style=cIsDamage>igniting</style> nearby enemies") : "") + "." + ((MageEruption.Stocks.Value != 1) ? string.Format(" Holds up to {0} charges.", MageEruption.Stocks.Value) : ""));

            SkillDef skillDef = ScriptableObject.CreateInstance<SkillDef>();
            skillDef.activationState = new SerializableEntityStateType(typeof(MageEruptionState));
            skillDef.activationStateMachineName = "Body";
            skillDef.baseMaxStock = Mathf.Max(MageEruption.Stocks.Value, 1);
            skillDef.baseRechargeInterval = Mathf.Max(MageEruption.Cooldown.Value, 0.5f);
            skillDef.beginSkillCooldownOnSkillEnd = false;
            skillDef.canceledFromSprinting = false;
            skillDef.forceSprintDuringState = MageEruption.IsSprint.Value;
            skillDef.cancelSprintingOnActivation = !MageEruption.IsSprint.Value;
            skillDef.isCombatSkill = (MageEruption.BlastArea.Value > 0f);
            skillDef.fullRestockOnAssign = true;
            skillDef.interruptPriority = InterruptPriority.PrioritySkill;
            skillDef.mustKeyPress = true;
            skillDef.rechargeStock = 1;
            skillDef.requiredStock = 1;
            skillDef.stockToConsume = 1;
            skillDef.icon = MageEruption.SpriteFromFile("eruption.png");
            skillDef.skillName = "MAGE_ERUPTION_FIRE";
            skillDef.skillNameToken = "MAGE_ERUPTION_FIRE";
            skillDef.skillDescriptionToken = "MAGE_ERUPTION_DESCRIPTION";
            GameObject mageBodyprefab = Addressables.LoadAssetAsync<GameObject>("mage").WaitForCompletion();
            bool wasAdded;
            ContentAddition.AddEntityState(typeof(MageEruptionState), out wasAdded);
            ContentAddition.AddSkillDef(skillDef);

            //idk why I can't put the if statement up here instead but whatever this works
            SkillFamily skillFamily = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Mage/MageBodySpecialFamily.asset").WaitForCompletion();
            SkillFamily skillFamily2 = Addressables.LoadAssetAsync<SkillFamily>("RoR2/Base/Mage/MageBodyUtilityFamily.asset").WaitForCompletion();
            
            if (IsSpecial.Value)
            {
                Array.Resize<SkillFamily.Variant>(ref skillFamily.variants, skillFamily.variants.Length + 1);
                SkillFamily.Variant[] variants = skillFamily.variants;
                int num = skillFamily.variants.Length - 1;
                SkillFamily.Variant variant = default(SkillFamily.Variant);
                variant.skillDef = skillDef;
                variant.unlockableDef = null;
                variant.viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null);
                variants[num] = variant;
            }
            else
            {
                Array.Resize<SkillFamily.Variant>(ref skillFamily2.variants, skillFamily2.variants.Length + 1);
                SkillFamily.Variant[] variants2 = skillFamily2.variants;
                int num2 = skillFamily2.variants.Length - 1;
                SkillFamily.Variant variant = default(SkillFamily.Variant);
                variant.skillDef = skillDef;
                variant.unlockableDef = null;
                variant.viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null);
                variants2[num2] = variant;
            }
        }
        public static Sprite SpriteFromFile(string name)
        {
            Texture2D texture2D = new Texture2D(2, 2);
            try
            {
                ImageConversion.LoadImage(texture2D, File.ReadAllBytes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), name)));
            }
            catch (FileNotFoundException ex)
            {
                Debug.LogError("Failed to read file at " + ex.FileName);
                return null;
            }
            return Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.height, (float)texture2D.width), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
