namespace FathomlessVoidling.EntityStates.Utility
{
    public class EnterMaze : BaseMazeAttackState
    {
        public string animLayerName = "Body";
        public string animStateName = "ChargeGravityBump";
        public string animPlaybackRateParamName = "GravityBump.playbackRate";
        public float baseDuration = 2f;
        private float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration;
            if (!string.IsNullOrEmpty(this.animLayerName) && !string.IsNullOrEmpty(this.animStateName))
            {
                if (!string.IsNullOrEmpty(this.animPlaybackRateParamName))
                    this.PlayAnimation(this.animLayerName, this.animStateName, this.animPlaybackRateParamName, this.duration);
                else
                    this.PlayAnimation(this.animLayerName, this.animStateName);
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if ((double)this.fixedAge < this.duration || !this.isAuthority)
                return;
            this.outer.SetNextState(new MazeAttack());
        }
    }
}