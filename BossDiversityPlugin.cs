using BepInEx;
using BepInEx.Configuration;
using RiskOfOptions.Options;
using RiskOfOptions;
using RoR2;
using UnityEngine;
using System.IO;

namespace BossDiversity
{
    [BepInDependency("com.rune580.riskofoptions")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class BossDiversityPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "Lawlzee.BossDiversity";
        public const string PluginAuthor = "Lawlzee";
        public const string PluginName = "BossDiversity";
        public const string PluginVersion = "1.0.0";

        private bool _isBossWave;

        public static ConfigEntry<bool> ModEnabled;

        public void Awake()
        {
            Log.Init(Logger);

            On.RoR2.CombatDirector.SetNextSpawnAsBoss += CombatDirector_SetNextSpawnAsBoss;
            On.RoR2.CombatDirector.Simulate += CombatDirector_Simulate;
            On.RoR2.BossGroup.OnMemberDefeatedServer += BossGroup_OnMemberDefeatedServer; ;

            ModEnabled = Config.Bind("Configuration", "Mod enabled", true, "Mod enabled");
            
            ModSettingsManager.AddOption(new CheckBoxOption(ModEnabled));
            ModSettingsManager.SetModIcon(LoadIconSprite());
        }

        private Sprite LoadIconSprite()
        {
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(File.ReadAllBytes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "icon.png")));
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));

        }

        private void CombatDirector_SetNextSpawnAsBoss(On.RoR2.CombatDirector.orig_SetNextSpawnAsBoss orig, CombatDirector self)
        {
            orig(self);

            if (ModEnabled.Value && !_isBossWave && TeleporterInteraction.instance?.bossDirector == self)
            {
                Log.Debug("Spawn wave started");
                _isBossWave = true;
            }
            
        }

        private void CombatDirector_Simulate(On.RoR2.CombatDirector.orig_Simulate orig, CombatDirector self, float deltaTime)
        {
            orig(self, deltaTime);

            if (ModEnabled.Value)
            {
                if (_isBossWave && TeleporterInteraction.instance?.bossDirector == self)
                {
                    if (self.currentMonsterCard == null)
                    {
                        Log.Debug("Boss wave spawn ended");
                        _isBossWave = false;
                    }
                    else
                    {
                        Log.Debug("Replacing boss spawn");

                        float monsterSpawnTimer = self.monsterSpawnTimer;
                        int spawnCountInCurrentWave = self.spawnCountInCurrentWave;

                        self.SetNextSpawnAsBoss();

                        self.monsterSpawnTimer = monsterSpawnTimer;
                        self.spawnCountInCurrentWave = spawnCountInCurrentWave;
                    }
                }
            }
            else
            {
                _isBossWave = false;
            }

        }

        private void BossGroup_OnMemberDefeatedServer(On.RoR2.BossGroup.orig_OnMemberDefeatedServer orig, BossGroup self, CharacterMaster memberMaster, DamageReport damageReport)
        {
            orig(self, memberMaster, damageReport);

            if (!ModEnabled.Value)
            {
                return;
            }

            for (int index = 0; index < self.bossMemoryCount; ++index)
            {
                BossGroup.BossMemory memory = self.bossMemories[index];
                if (memory.cachedBody.healthComponent.alive)
                {
                    self.bestObservedName = Util.GetBestBodyName(memory.cachedBody.gameObject);
                    self.bestObservedSubtitle = memory.cachedBody.GetSubtitle();
                    if (self.bestObservedSubtitle.Length == 0)
                        self.bestObservedSubtitle = Language.GetString("NULL_SUBTITLE");
                    self.bestObservedSubtitle = "<sprite name=\"CloudLeft\" tint=1> " + self.bestObservedSubtitle + "<sprite name=\"CloudRight\" tint=1>";

                    Log.Debug("Health bar label updated");
                    break;
                }
            }
        }
    }
}
