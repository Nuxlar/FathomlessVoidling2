using RoR2;
using EntityStates;

namespace FathomlessVoidling.EntityStates
{
    public class JointSpawnState : BaseState
    {
        public float duration = 4.5f;

        private bool visible = false;
        private CharacterModel characterModel;

        public override void OnEnter()
        {
            base.OnEnter();
            this.characterModel = this.GetModelTransform().GetComponent<CharacterModel>();
            if ((bool)this.characterModel)
                ++this.characterModel.invisibilityCount;
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if ((double)this.fixedAge >= (double)this.duration && !visible)
            {
                if ((bool)this.characterModel)
                    --this.characterModel.invisibilityCount;
                this.visible = true;
            }
            if ((double)this.fixedAge < (double)this.duration || !this.isAuthority)
                return;
            this.outer.SetNextStateToMain();
        }
    }
}