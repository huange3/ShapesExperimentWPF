using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;

namespace ShapesExperimentWPF
{
    public class Sounds
    {
        public SoundPlayer Player;

        public Sounds()
        {
            Player = new SoundPlayer();
        }

        public void playSuccess()
        {
            Player.Stream = Properties.Resources.coins;
            Player.Play();
        }

        public void playMiss()
        {
            Player.Stream = Properties.Resources.glassBreak;
            Player.Play();
        }

        public void playReward()
        {
            Player.Stream = Properties.Resources.cashRegister;
            Player.Play();
        }

        public void playNoReward()
        {
            Player.Stream = Properties.Resources.toneHit;
            Player.Play();
        }
    }
}
