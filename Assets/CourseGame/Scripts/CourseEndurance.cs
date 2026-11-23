#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CourseGame
{
    public sealed class CourseEndurance : MonoBehaviour
    {
        IEnumerator Start()
        {
            var s=CourseSession.Instance;
            s.VerificationRun=true;
            if(s.State!=CourseSession.Phase.Welcome)yield break;
            string p="CourseGameVerification/Endurance-v01.txt";
            File.WriteAllText(p,"Real-time 180-second gameplay run\n"+DateTime.Now.ToString("s")+"\nNo accelerated timer, no injected health or damage. Keyboard movement follows a square.\n");
            s.StartRun();var keys=new[]{Key.W,Key.D,Key.S,Key.A};int last=-1;int report=-1;
            float realStart=Time.realtimeSinceStartup;
            var kb=Keyboard.current;
            while(s.State==CourseSession.Phase.Playing)
            {
                if(s.IsChoosingReward)s.rewards.Choose(PlayerUpgradeType.Damage);
                int step=Mathf.FloorToInt(s.Elapsed/5f)%4;
                if(step!=last){InputSystem.QueueStateEvent(kb,new KeyboardState(keys[step]));last=step;}
                int mark=Mathf.FloorToInt(s.Elapsed/30);
                if(mark!=report)
                {
                    report=mark;
                    File.AppendAllText(p,$"time={s.Elapsed:F1} realtime={Time.realtimeSinceStartup-realStart:F1} hp={s.player.CurrentHealth} kills={s.Kills} level={s.Level} shots={s.Shots} enemies={GameManager.Instance.EnemyCount} created={GameManager.Instance.GetEnemyPool().CreatedCount}\n");
                }
                yield return null;
            }
            InputSystem.QueueStateEvent(kb,new KeyboardState());
            bool success=s.State==CourseSession.Phase.Victory && s.Elapsed>=180;
            File.AppendAllText(p,(success?"PASS":"FAIL")+$" final={s.State} elapsed={s.Elapsed:F2} realtime={Time.realtimeSinceStartup-realStart:F2} hp={s.player.CurrentHealth} kills={s.Kills} shots={s.Shots}\nCOMPLETE\n");
            ScreenCapture.CaptureScreenshot("CourseGameVerification/Victory-preview-v01.png");
        }
    }
}
#endif
