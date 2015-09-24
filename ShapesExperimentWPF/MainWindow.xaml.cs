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
using System.IO;

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
        public string ParticipantID = "";

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
            ParticipantID = result;
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
                newBoard.ParticipantID = this.ParticipantID;

                newBoard.Show();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while running experiment: " + e.Message);
                throw e;
            }
        }

        private void generateIDBtn_Click(object sender, RoutedEventArgs e)
        {
            generateID();
        }

        public void outputData()
        {
            StringBuilder builder = new StringBuilder();
            var filePath = "";
            var newLine = "";

            try
            {
                // check if our directory exists, if it doesn't then create it
                if (!Directory.Exists("./Data"))
                {
                    Directory.CreateDirectory("./Data");
                }

                filePath = String.Format("{0}{1}-{2}.txt", "./Data/", DateTime.Now.ToString("yyyyMMdd"), ParticipantID);

                foreach (Phase p in Phases)
                {
                    // start with our phase
                    newLine = "Condition,Background Color,Observations,Density,Rank Type,Response Index,Trial Duration,Trial Rest Duration\n";
                    newLine += String.Format("{0},{1},{2},{3},{4},{5},{6},{7}{8}{9}",
                        p.Label,
                        p.BackgroundColor.ToString(),
                        p.Observations,
                        p.Density,
                        p.RankType,
                        p.ResponseIndex,
                        TrialDuration,
                        TrialRestDuration,
                        Environment.NewLine,
                        Environment.NewLine);

                    // now write the trial data
                    newLine += "Success Count,Miss Count,Money,Response Value\n";

                    foreach (Trial t in p.Trials)
                    {
                        newLine += String.Format("{0},{1},{2},{3}{4}",
                            t.SuccessCount,
                            t.MissCount,
                            t.Money,
                            t.ResponseValue,
                            Environment.NewLine);
                    }

                    newLine += "\n";
                    builder.Append(newLine);
                }

                File.WriteAllText(filePath, builder.ToString());
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while writing data to file: " + e.Message);
                throw e;
            }
            finally
            {
                builder = null;
            }
        }
    }
}
