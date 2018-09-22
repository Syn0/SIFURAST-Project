using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.Events;

//[SharedBetweenAnimators]
public class OnAnimationEnd : StateMachineBehaviour {
    public int delay = 1000;
    public Animator animator;
    int? nextStateVal = null;

    private void Awake()
    {
    }
    

    // OnStateEnter is called before OnStateEnter is called on any state inside this state machine
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        delay = (int)(stateInfo.length * 1000f);
        Task.Delay(delay).ContinueWith(t => nextStateVal = 0);
    }
    void ResetStateValue()
    {
        animator.SetInteger("stateVal", 0);

    }

    /*
          UnityEditorInternal.AnimatorController ac = anim.runtimeAnimatorController as UnityEditorInternal.AnimatorController;
     UnityEditorInternal.StateMachine sm = ac.GetLayer(0).stateMachine;
     
     for(int i = 0; i < sm.stateCount; i++) {
         UnityEditorInternal.State state = sm.GetState(i);
         if(state.uniqueName == track) {
             AnimationClip clip = state.GetMotion() as AnimationClip;
             if(clip != null) {
                 length = clip.length;
             }
         }
     }
     Debug.Log("Animation:"+track+":"+length);
     */
    // OnStateUpdate is called before OnStateUpdate is called on any state inside this state machine
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
    //}

    // OnStateExit is called before OnStateExit is called on any state inside this state machine
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
    //}

    // OnStateMove is called before OnStateMove is called on any state inside this state machine
    override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (nextStateVal.HasValue)
        {
            animator.SetInteger("stateVal", nextStateVal.Value);
            nextStateVal = null;
        }

    }

    // OnStateIK is called before OnStateIK is called on any state inside this state machine
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
    //
    //}

    // OnStateMachineEnter is called when entering a statemachine via its Entry Node
    //override public void OnStateMachineEnter(Animator animator, int stateMachinePathHash){
    //
    //}

    // OnStateMachineExit is called when exiting a statemachine via its Exit Node
    //override public void OnStateMachineExit(Animator animator, int stateMachinePathHash) {
    //
    //}
}
