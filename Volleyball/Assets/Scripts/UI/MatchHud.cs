using UnityEngine;
using UnityEngine.UI;
namespace Volleyball
{
    public sealed class MatchHud : MonoBehaviour
    {
        public MatchController Match;
        public Text Score;
        public Text Status;
        public Text Result;
        public GameObject ResultPanel;
        public Button RestartButton;
        void Start() {Match.Changed+=Refresh;RestartButton.onClick.AddListener(Match.Restart);Refresh();}
        void OnDestroy() {if(Match) Match.Changed-=Refresh; if(RestartButton && Match) RestartButton.onClick.RemoveListener(Match.Restart);}
        void Refresh()
        {
            if(Match.Rules==null) return;
            Score.text=$"PLAYER  {Match.Rules.HumanScore}    :    {Match.Rules.CpuScore}  CPU";
            Status.text=Match.LastMessage;
            bool finished=Match.Rules.State==MatchState.MatchFinished;ResultPanel.SetActive(finished);
            Result.text=Match.Rules.HumanScore>=Match.Settings.winningScore?"YOU WIN":"CPU WINS";
        }
    }
}
