using Game.Simulation;namespace Game.Runtime {internal static class SourceS002ContactCompletion {internal static void Complete(ref AriaS002ContactState current,int frame,float now,bool hasSkirmish){
                    current.Actions++;
                    current.LastCompletedTargetId=current.TargetId;
                    current.LastCompletedFrame=frame;
                    current.LastCompletedPosition=current.Contact;
                    current.Phase = AriaS002ContactPhase.Verifying;
                    current.DueAt = now + (hasSkirmish ? .65f : 1.5f);}}}
