using EntityStates;
using RoR2;
using UnityEngine;

namespace FathomlessVoidling.EntityStates.Joint
{
    public class JointDeathNux : GenericCharacterDeath
    {
        public string joint1Name = "Calf";
        public string joint2Name = "Foot";
        public string joint3Name = "Toe";
        public GameObject joint1EffectPrefab = Main.jointBreakEffect;
        public GameObject joint2EffectPrefab = Main.jointBreakEffect;
        public GameObject joint3EffectPrefab = Main.jointBreakEffect;
        private CharacterModel characterModel;

        public override void OnEnter()
        {
            base.OnEnter();
            EffectManager.SimpleMuzzleFlash(this.joint1EffectPrefab, this.gameObject, this.joint1Name, false);
            EffectManager.SimpleMuzzleFlash(this.joint2EffectPrefab, this.gameObject, this.joint2Name, false);
            EffectManager.SimpleMuzzleFlash(this.joint3EffectPrefab, this.gameObject, this.joint3Name, false);
            Transform modelTransform = this.GetModelTransform();
            if (modelTransform)
                this.characterModel = modelTransform.GetComponent<CharacterModel>();
            if (!this.characterModel)
                return;
            ++this.characterModel.invisibilityCount;
        }

        public override void OnExit()
        {
            if (this.characterModel)
                --this.characterModel.invisibilityCount;
            base.OnExit();
        }
    }
}
