using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Shapes;

namespace ShapesExperimentWPF
{
    /// <summary>
    /// Interaction logic for TestModeBoard.xaml
    /// </summary>
    public partial class TestModeBoard : Window
    {
        public List<Phase> Phases = new List<Phase>();
        public Queue<Phase> PhaseQueue = null;
        public Phase PhaseATemplate = null;
        public Phase PhaseBTemplate = null;
        public Phase PhaseCTemplate = null;
        public int TrialDuration = 0;
        public int TrialRestDuration = 0;
        public int PhaseRestDuration = 0;
        public decimal MoneyValue;
        public decimal RewardValue;
        public string ParticipantID = "";
        public int BaselineObservationValue = 0;
        public int BObservationValue = 0;
        public int CObservationValue = 0;
        public int MaxTrialValue = 0;
        public int TotalPhasesValue = 0;

        private Phase CurrentPhase = null;
        private Trial CurrentTrial = null;
        private List<int> ObservationList = new List<int>();

        private int CurrentTrialCount = 0;
        private bool RewardOn = false;
        private bool endPhase = false;

        public TestModeBoard()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            generateID();
        }

        private void generateIDBtn_Click(object sender, RoutedEventArgs e)
        {
            generateID();
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

        private bool setupPhases()
        {
            var phaseObservationCount = 0;
            var baselineObservationCount = 0;
            decimal currBDensity;
            decimal currCDensity;

            try
            {
                // Initialize and set up phases
                // The response index is calculated upon new object construction
                if (Phases.Count > 0) Phases.Clear();

                Color currAColor = (Color)colorPickerA.SelectedColor;
                Color currBColor = (Color)colorPickerB.SelectedColor;
                Color currCColor = (Color)colorPickerC.SelectedColor;

                TrialDuration = (int)trialDurationUD.Value;
                TrialRestDuration = (int)trialRestDurationUD.Value;
                PhaseRestDuration = (int)phaseRestDurationUD.Value;
                MoneyValue = (decimal)startingAmountUD.Value;
                RewardValue = (decimal)rewardIncrementUD.Value;

                BaselineObservationValue = (int)observationsBaselineUD.Value;
                BObservationValue = (int)observationsBUD.Value;
                CObservationValue = (int)observationsCUD.Value;
                baselineObservationCount = 0;
                phaseObservationCount = 0;
                currBDensity = (decimal)BDensityUD.Value;
                currCDensity = (decimal)CDensityUD.Value;
                MaxTrialValue = (int)maxTrialsUD.Value;
                TotalPhasesValue = (int)totalPhasesUD.Value;

                PhaseATemplate = new Phase(Constants.PhaseBaseline,
                            currAColor,
                            BaselineObservationValue,
                            0,
                            Constants.NoRank,
                            baselineObservationCount);

                PhaseBTemplate = new Phase(Constants.PhaseB,
                            currBColor,
                            BObservationValue,
                            currBDensity,
                            Constants.LessThan,
                            phaseObservationCount);

                PhaseCTemplate = new Phase(Constants.PhaseC,
                            currCColor,
                            CObservationValue,
                            currCDensity,
                            Constants.GreaterThan,
                            phaseObservationCount);

                PhaseQueue = new Queue<Phase>();
                PhaseQueue.Enqueue(new Phase(PhaseATemplate));

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while setting up experiment: " + e.Message);
                return false;
                throw e;
            }
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            toggleButtons(true);

            richTextBoxTestMain.Document.Blocks.Clear();

            if (setupPhases()) runPhase();
        }

        public bool runPhase()
        {
            try
            {
                endPhase = false;

                if (PhaseQueue.Count == 0 && Phases.Count == 0)
                {
                    MessageBox.Show("Invalid phase information received. Please try again.");
                    toggleButtons(false);
                    return false;
                }

                // End our experiment!!
                if (PhaseQueue.Count == 0 || Phases.Count == TotalPhasesValue)
                {
                    printLog("End of experiment! Printing test data to file...");
                    MessageBox.Show("Test experiment ended! Writing test data to file...");
                    outputTestData();
                    toggleButtons(false);
                    return false;
                }

                CurrentPhase = PhaseQueue.Dequeue();

                printPhaseInformation();
                printLog("Waiting for input...");

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while starting phase: " + e.Message);
                this.Close();
                return false;
                throw e;
            }
        }

        private void printPhaseInformation()
        {
            Run currRun = new Run();
            currRun.Foreground = Brushes.Blue;
            currRun.Text = "Current Phase: " + CurrentPhase.Label + ", Observations (m)=" + CurrentPhase.Observations + ", Density (w)=" + CurrentPhase.Density;

            richTextBoxTestMain.Document.Blocks.Add(new Paragraph(currRun));
            richTextBoxTestMain.ScrollToEnd();
        }

        private void printTrialInformation()
        {
            Run currRun = new Run();
            currRun.Foreground = Brushes.DarkMagenta;
            currRun.Text = "Phase " + CurrentPhase.Label + ", Trial " + CurrentTrialCount + " completed! Success count=" + CurrentTrial.SuccessCount;

            richTextBoxTestMain.Document.Blocks.Add(new Paragraph(currRun));
            richTextBoxTestMain.ScrollToEnd();
        }

        private void printLog(String text, Brush color = null)
        {
            Run currRun = new Run();

            if (color == null) color = Brushes.Black;

            currRun.Foreground = color;
            currRun.Text = text;

            richTextBoxTestMain.Document.Blocks.Add(new Paragraph(currRun));
            richTextBoxTestMain.ScrollToEnd();
        }

        private void runTrial()
        {
            try
            {
                CurrentTrial = new Trial();
                CurrentTrialCount++;

                CurrentTrial.SuccessCount = (int)successCountUD.Value;

                printTrialInformation();

                printLog("Calculating response...");

                calculateResponse();

                printLog("Waiting for input...");
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while starting trial: " + e.Message);
                throw e;
            }
        }

        private void calculateResponse()
        {
            List<int> successValues = new List<int>();
            int responseIndex = 0;

            try
            {
                // If this is the baseline phase, then skip calculations and just add to list!
                // Otherwise, use our latest m trials to calculate the response index.
                // If the current number of trials in this phase are less than the required 
                // observations, then fill from the baseline.
                // variable ObservationList is a running list of trial success counts, only 
                // cleared upon entering a new baseline phase (Phase A).
                //
                // Sort the success values according to the phase rank
                // Get our response index from the current phase info
                // Reward the user if our current success count satisfies the rank criteria
                // either greater than or less than the response index
                // k = (m + 1)(1 - w)

                // Removing the observation list limit, just keep stacking it up!
                ObservationList.Add(CurrentTrial.SuccessCount);

                if (CurrentPhase.Label == Constants.PhaseBaseline)
                {
                    CurrentPhase.Trials.Add(CurrentTrial);
                    RewardOn = false;

                    printLog("No response needed for baseline phase!");
                }
                else
                {
                    // Change of plans, do normal rounding for the response index. 
                    // ex. 10.5 => 11, 2.5 => 3, 5.2 => 5
                    responseIndex = (int)Math.Round((CurrentPhase.Observations + 1) * (1 - CurrentPhase.Density), MidpointRounding.AwayFromZero);

                    // get our sub-list of the observation list, and 
                    // sort in ascending order
                    // Note: Do NOT include the most recent timing in our calculations,
                    // the most recent timing is the one we're testing against!
                    for (int o = CurrentPhase.Observations + 1; o > 1; o--)
                    {
                        successValues.Add(ObservationList[ObservationList.Count - o]);
                    }

                    successValues.Sort();

                    string currText = "Sorted observation list: ";
                    string separator = "";

                    foreach (int val in successValues)
                    {
                        currText += separator + val.ToString();
                        separator = ", ";
                    }

                    printLog(currText);
                    printLog("Response value=" + successValues[responseIndex - 1]);

                    if (CurrentPhase.RankType == Constants.LessThan)
                    {
                        if (CurrentTrial.SuccessCount < successValues[responseIndex - 1])
                        {
                            MoneyValue += RewardValue;
                            RewardOn = true;
                        }
                        else RewardOn = false;

                    }
                    else if (CurrentPhase.RankType == Constants.GreaterThan)
                    {
                        if (CurrentTrial.SuccessCount > successValues[responseIndex - 1])
                        {
                            MoneyValue += RewardValue;
                            RewardOn = true;
                        }
                        else RewardOn = false;
                    }

                    // save our current trial data and add it to our current phase's trial list
                    CurrentTrial.ResponseValue = successValues[responseIndex - 1];
                    CurrentTrial.Money = MoneyValue;
                    CurrentPhase.Trials.Add(CurrentTrial);

                    //// update our money label
                    //moneyLB.Content = MoneyValue.ToString("C");

                    if (RewardOn)
                    {
                        printLog("Money awarded! Reward=" + RewardValue + ", Total ($)=" + MoneyValue, Brushes.Green);
                    }
                    else
                    {
                        printLog("Failed to meet response value. Total ($)=" + MoneyValue, Brushes.Red);
                    }
                }

                // Determine our phase order and trial stability
                // Rules:
                // Phase A: ends when 5 most recent timings have stability OR celeration rate of x1
                // Phase B: ends when 5 most recent timings have stability AND celeration rate of % VALUE
                // OR 5 consecutive timings of 0
                // Phase C: ends when 5 most recent timings have stability AND celeration rate of x VALUE
                // OR 5 consecutive timings of 0
                double celerationVal = 0.0;

                // ////////////////////////////////////////////////////////////////
                if (CurrentTrialCount >= CurrentPhase.Observations)
                {
                    celerationVal = calculateCelerationValue();

                    printLog("Celeration value=" + celerationVal, Brushes.ForestGreen);

                    switch (CurrentPhase.Label)
                    {
                        case 'A':
                            if (celerationVal == 1 || checkStability())
                            {
                                // Phase A ends! Decide on where to send them next...
                                // Negative celeration value goes to Phase C
                                // Positive celeration value goes to Phase B
                                endPhase = true;

                                CurrentPhase.CelerationValue = celerationVal;

                                if (celerationVal < 0)
                                {
                                    PhaseQueue.Enqueue(new Phase(PhaseCTemplate));
                                }
                                else
                                {
                                    PhaseQueue.Enqueue(new Phase(PhaseBTemplate));
                                }
                            }
                            break;

                        case 'B':
                            if ((celerationVal < 0 && checkStability()) || checkZeroSum())
                            {
                                endPhase = true;

                                CurrentPhase.CelerationValue = celerationVal;

                                PhaseQueue.Enqueue(new Phase(PhaseCTemplate));
                            }
                            break;

                        case 'C':
                            if ((celerationVal > 0 && checkStability()) || checkZeroSum())
                            {
                                endPhase = true;

                                CurrentPhase.CelerationValue = celerationVal;

                                PhaseQueue.Enqueue(new Phase(PhaseBTemplate));
                            }
                            break;

                        default:
                            break;
                    }

                    // Decide if we're going to end our phase
                    if (CurrentTrialCount >= MaxTrialValue)
                    {
                        endPhase = true;
                    }

                    if (endPhase)
                    {
                        printLog("Ending Phase " + CurrentPhase.Label + "...", Brushes.Blue);
                        CurrentTrialCount = 0;
                        Phases.Add(CurrentPhase);

                        runPhase();
                    }
                }

            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while calculating response value: " + e.Message);
            }
            finally
            {
                successValues = null;
            }
        }

        private bool checkZeroSum()
        {
            List<int> currCounts = new List<int>();

            for (int i = 1; i <= 5; i++)
            {
                currCounts.Add(CurrentPhase.Trials[CurrentPhase.Trials.Count - i].SuccessCount);
            }

            int sum = 0;

            for (int i = 0; i < currCounts.Count; i++)
            {
                sum += currCounts[i];
            }

            if (sum == 0) return true;

            return false;
        }

        private bool checkStability()
        {
            try
            {
                // Get the latest 5 timings of this phase and order their 
                // success count from least to greatest, find the median.
                // Check if the other values fall within 20% of this median.
                // If yes, then stability is reached - return true.
                // Else, return false.
                // OR if they score 0 for all 5 timings
                List<int> currCounts = new List<int>();

                for (int i = 1; i <= 5; i++)
                {
                    currCounts.Add(CurrentPhase.Trials[CurrentPhase.Trials.Count - i].SuccessCount);
                }

                // If all zeroes, then return stable! /////////////////
                int sum = 0;

                for (int i = 0; i < currCounts.Count; i++)
                {
                    sum += currCounts[i];
                }

                if (sum == 0) return true;

                ////////////////////////////////////////

                currCounts.Sort();

                double median = 0.0;
                double variance = 0.0;
                bool isStable = false;

                // check if it's an even number
                if (currCounts.Count % 2 == 0)
                {
                    median = (currCounts[currCounts.Count / 2] + currCounts[currCounts.Count / 2 - 1]) / 2;
                }
                else
                {
                    median = currCounts[(int)Math.Floor((decimal)(currCounts.Count / 2))];
                }

                variance = median * 0.20;

                foreach (int x in currCounts)
                {
                    if (x >= (median - variance) && x <= (median + variance))
                    {
                        isStable = true;
                    }
                    else
                    {
                        isStable = false;
                        break;
                    }
                }

                printLog("Stability=" + isStable.ToString(), Brushes.ForestGreen);

                return isStable;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while checking stability: " + e.Message);
                throw e;
            }
        }

        private double calculateCelerationValue()
        {
            try
            {
                // First step: Find the linear regression of the logs
                // Get the latest 5 timings of this phase.
                // x = [1, 2, 3, 4, 5 ]
                // y = [successCount1, successCount2, successCount3, ... ]
                List<int> currCounts = new List<int>();
                List<int> x = new List<int>();
                List<double> yLogs = new List<double>();

                for (int i = 5; i > 0; i--)
                {
                    currCounts.Add(CurrentPhase.Trials[CurrentPhase.Trials.Count - i].SuccessCount);
                }

                for (int i = 1; i <= 5; i++)
                {
                    x.Add(i);
                }

                // Get our list of Log Y values
                foreach (int i in currCounts)
                {
                    yLogs.Add(Math.Log10(i));
                }

                // Calculate sigma XY
                double sigXY = 0.0;

                for (int i = 0; i < 5; i++)
                {
                    sigXY += (x[i] * yLogs[i]);
                }

                // Calculate N sigma XY
                double nSigXY = 5 * sigXY;

                // Calculate sum of X
                int sumX = 0;

                foreach (var i in x)
                {
                    sumX += i;
                }

                // Calculate sum of Y logs
                double sumYLogs = 0.0;

                foreach (double i in yLogs)
                {
                    sumYLogs += i;
                }

                // Calculate N sig XY - (sig X * sig Y Logs)
                double slope = nSigXY - (sumX * sumYLogs);

                // Calculate N sigma X squared
                int nSigXSquared = 0;

                foreach (int i in x)
                {
                    nSigXSquared += (i * i);
                }

                nSigXSquared = 5 * nSigXSquared;

                // Calculate the final slope value
                slope = slope / (nSigXSquared - (sumX * sumX));

                // Now onto the Y intercept
                double yIntercept = (sumYLogs - (slope * sumX)) / 5;

                // Second step: Calculate the actual celeration value
                // Will be using positive numbers for x VALUE celeration,
                // negative numbers for % VALUE celeration for convenience sake
                double yVal = slope * x[0] + yIntercept;
                double startFreq = Math.Pow(10, yVal);

                yVal = slope * x[5 - 1] + yIntercept;
                double endFreq = Math.Pow(10, yVal);

                double celerationVal = Math.Pow(endFreq / startFreq, 0.2);

                celerationVal = Math.Pow(celerationVal, 7);

                if (celerationVal < 1)
                {
                    celerationVal = (1 / celerationVal) * -1; // making it negative instead of % VALUE for easier parsing
                }

                return Math.Round(celerationVal, 2); // phew, done!
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while calculating celeration value: " + e.Message);
                throw e;
            }
        }

        private void submitBtn_Click(object sender, RoutedEventArgs e)
        {
            runTrial();
            successCountUD.Focus();
        }

        public void outputTestData()
        {
            StringBuilder builder = new StringBuilder();
            var filePath = "";
            var newLine = "";

            try
            {
                // check if our directory exists, if it doesn't then create it
                if (!Directory.Exists("./TestData"))
                {
                    Directory.CreateDirectory("./TestData");
                }

                filePath = String.Format("{0}{1}-{2}-{3}.csv", "./TestData/", "Test", DateTime.Now.ToString("yyyyMMdd"), ParticipantID);

                // state our limiters
                newLine += "Max Trial Value,Total Phases\n";
                newLine += String.Format("{0},{1}{2}{3}",
                    MaxTrialValue,
                    TotalPhasesValue,
                    Environment.NewLine,
                    Environment.NewLine);

                foreach (Phase p in Phases)
                {
                    // start with our phase
                    newLine += "Phase,Background Color,Observations,Density,Rank Type,Trial Time,Trial Rest Time,Phase Rest Time,Number of Trials\n";
                    newLine += String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}{9}{10}",
                        p.Label,
                        p.BackgroundColor.ToString(),
                        p.Observations,
                        p.Density,
                        p.RankType,
                        TrialDuration,
                        TrialRestDuration,
                        PhaseRestDuration,
                        p.TrialCount,
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
                    newLine += "Celeration Value\n";
                    newLine += p.CelerationValue + "\n\n";
                }

                builder.Append(newLine);

                File.WriteAllText(filePath, builder.ToString());

                printLog("Test data file has been saved: " + filePath, Brushes.DeepPink);
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

        private void stopTestModeBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Test experiment ended! Writing test data to file...");
            printLog("Stopped experiment! Printing test data to file...");
            outputTestData();

            toggleButtons(false);
        }

        private void toggleButtons(bool isStart)
        {
            if (isStart)
            {
                stopTestModeBtn.IsEnabled = true;
                startTestModeBtn.IsEnabled = false;
                submitBtn.IsEnabled = true;
            } else
            {
                stopTestModeBtn.IsEnabled = false;
                startTestModeBtn.IsEnabled = true;
                submitBtn.IsEnabled = false;
            }
        }
    }
}
