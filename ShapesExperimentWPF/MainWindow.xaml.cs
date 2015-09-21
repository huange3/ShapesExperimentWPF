using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;

namespace ShapesExperimentWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<Phase> Phases;
        public int TrialDuration = 0;
        public int TrialRestDuration = 0;
        public decimal MoneyValue;
        public decimal RewardValue;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            generateID();
        }

        private void startBtn_Click(object sender, EventArgs e)
        {
            Regex re = new Regex(@"(^[abc]+$)/i");

            if (conditionOrderTB.Text == "")
            {
                MessageBox.Show("Invalid condition order entered. Please try again.");
                return;
            }

            if (re.IsMatch(conditionOrderTB.Text))
            {
                MessageBox.Show("Invalid condition order entered. Please try again.");
                return;
            }

            if (participantIDTB.Text == "")
            {
                MessageBox.Show("Invalid participant ID entered. Please try again.");
                return;
            }

            if (setupPhases()) viewBoard();
        }

        private void generateID()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new string(
                Enumerable.Repeat(chars, 8)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());

            participantIDTB.Text = result;
        }

        private Boolean setupPhases()
        {
            var conditionOrderStr = "";
            var observationCount = 0;
            decimal currBDensity;
            decimal currCDensity;
            Phase currPhase = null;
            char currChar;

            try
            {
                // Initialize and set up phases
                // The response index is calculated upon new object construction
                Phases = new List<Phase>();

                conditionOrderStr = conditionOrderTB.Text;

                Color currAColor = (Color)colorPickerA.SelectedColor;
                Color currBColor = (Color)colorPickerB.SelectedColor;
                Color currCColor = (Color)colorPickerC.SelectedColor;

                TrialDuration = (int)trialDurationUD.Value;
                TrialRestDuration = (int)trialRestDurationUD.Value;
                MoneyValue = (decimal)startingAmountUD.Value;
                RewardValue = (decimal)rewardIncrementUD.Value;

                observationCount = (int)observationsUD.Value;
                currBDensity = (decimal)BDensityUD.Value;
                currCDensity = (decimal)CDensityUD.Value;

                foreach (char c in conditionOrderStr)
                {
                    currChar = Char.ToUpper(c);

                    if (currChar == Constants.PhaseBaseline)
                    {
                        currPhase = new Phase(Constants.PhaseBaseline, currAColor, observationCount, 0, Constants.NoRank);
                    }

                    if (currChar == Constants.PhaseB)
                    {
                        currPhase = new Phase(Constants.PhaseB, currBColor, observationCount, currBDensity, Constants.LessThan);
                    }

                    if (currChar == Constants.PhaseC)
                    {
                        currPhase = new Phase(Constants.PhaseC, currCColor, observationCount, currCDensity, Constants.GreaterThan);
                    }

                    Phases.Add(currPhase);
                }

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while setting up experiment: " + e.Message);
                return false;
                throw e;
            }
        }

        private void viewBoard()
        {
            mainBoard newBoard = null;
            try
            {
                // open our experiment window
                newBoard = new mainBoard();
                newBoard.PhaseQueue = new Queue<Phase>(this.Phases);
                newBoard.TrialDuration = this.TrialDuration;
                newBoard.TrialRestDuration = this.TrialRestDuration;
                newBoard.MoneyValue = this.MoneyValue;
                newBoard.RewardValue = this.RewardValue;

                newBoard.initializeBoard();
                newBoard.Show();
                newBoard.runPhase();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while running experiment: " + e.Message);
                throw e;
            }
        }
    }
}
