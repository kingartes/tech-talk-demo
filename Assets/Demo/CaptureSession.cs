using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace TechTalk
{
    internal sealed class CaptureSession : MonoBehaviour
    {
        private static readonly string[] Arguments = Environment.GetCommandLineArgs();
        internal static bool Requested => Array.IndexOf(Arguments,"-talkCapture")>=0;
        private static readonly WaitForEndOfFrame EndOfFrame = new();
        private DemoStateController demo;
        private string directory;

        private IEnumerator Start()
        {
            Application.runInBackground=true;
            demo=GetComponent<DemoStateController>();
            demo.SetExternalClock();
            int option=Array.IndexOf(Arguments,"-talkCapture");
            if(option+1>=Arguments.Length)
            {
                Debug.LogError("-talkCapture needs an output folder.");
                Application.Quit(2);
                yield break;
            }
            directory=Path.GetFullPath(Arguments[option+1]);
            Directory.CreateDirectory(directory);
            Debug.Log("Tech Talk capture started at "+Screen.width+"x"+Screen.height+".");
            yield return null;
            if(Array.IndexOf(Arguments,"-talkVideo")>=0) yield return Video();
            else yield return Stills();
            Debug.Log("Tech Talk capture complete.");
            Application.Quit();
        }

        private IEnumerator Stills()
        {
            for(int state=0;state<DemoStateController.StateCount;state++)
            {
                demo.Select(state);
                demo.ResetState();
                int steps=state==4?4:state==5||state==6?3:state==3?2:1;
                if(state==9) demo.StartFailure();
                for(int step=0;step<steps;step++)
                {
                    demo.Advance(state==9?12:.12f);
                    yield return EndOfFrame;
                    Write($"S{state:00}-{step}.png",false);
                    demo.CycleStep();
                    yield return null;
                }
            }
        }

        private IEnumerator Video()
        {
            const int fps=12;
            int frame=0;
            var cuts = new (int State,int Step,int Seconds)[] {
                (0,0,9),(7,0,8),(8,0,8),(9,0,15),(1,0,6),(2,0,6),(3,0,4),(3,1,6),
                (4,0,3),(4,1,4),(4,2,5),(4,3,4),(5,0,3),(5,1,3),(5,2,4),
                (6,1,4),(6,2,5),(7,0,4),(8,0,4),(9,0,15)
            };
            foreach(var cut in cuts)
            {
                demo.Select(cut.State);
                demo.ResetState();
                if(cut.State==9) demo.StartFailure();
                for(int s=0;s<cut.Step;s++) demo.CycleStep();
                for(int j=0;j<cut.Seconds*fps;j++)
                {
                    float t=j/(float)fps;
                    if(cut.State==2)
                    {
                        demo.Move=Mathf.Sin(t)*.8f;
                        demo.Turn=27+t*34;
                        demo.Size=1.5f+.4f*Mathf.Sin(t*1.7f);
                    }
                    if(cut.State==6 && cut.Step==2) demo.LightAngle=t*47;
                    demo.Advance(1f/fps);
                    yield return EndOfFrame;
                    Write($"f{frame++:00000}.jpg",true);
                }
            }
        }

        private void Write(string name,bool jpeg)
        {
            Texture2D texture=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(directory,name),jpeg?texture.EncodeToJPG(80):texture.EncodeToPNG());
            Destroy(texture);
        }
    }
}
