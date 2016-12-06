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
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using System.Media;

namespace ShapesExperimentWPF
{
    /// <summary>
    /// Interaction logic for mainBoard.xaml
    /// </summary>
    public partial class mainBoard : Window
    {
        public List<Phase> Phases = new List<Phase>();
        public Queue<Phase> PhaseQueue = null;
        public Phase PhaseATemplate = null;
        public Phase PhaseBTemplate = null;
        public Phase PhaseCTemplate = null;
        private Phase CurrentPhase = null;
        private Trial CurrentTrial = null;
        private List<Shape> BaselineShapes = null;
        private List<Shape> TrialShapes = null;
        private List<char> SkeletonBoard = new List<char>(64);
        private List<Image> ImageSet = new List<Image>();
        private Shape ShapeA = null;
        private Shape ShapeB = null;
        private Shape BucketA = null;
        private Shape BucketB = null;
        private List<int> ObservationList = new List<int>();

        public string ParticipantID = "";
        public int TrialDuration = 0;
        public int TrialRestDuration = 0;
        public int PhaseRestDuration = 0;
        public decimal MoneyValue;
        public decimal RewardValue;
        public int MaxTrialValue = 0;
        public int TotalPhasesValue = 0;
        private int CurrentTrialCount = 0;
        private int CurrentMillis = 0;      
        private bool BlinkOn = true;
        private bool RewardOn = false;

        static Random rand = new Random();
        private DispatcherTimer mainTimer = new DispatcherTimer();
        private DispatcherTimer restTimer = new DispatcherTimer();
        private DispatcherTimer phaseRestTimer = new DispatcherTimer();
        private DispatcherTimer rewardTimer = new DispatcherTimer();
        private DispatcherTimer hitTimer = new DispatcherTimer();

        private Image draggedImage;
        private Point mousePosition;
        private Point startMousePosition;
        private SoundPlayer RewardPlayer = null;
        private SoundPlayer NoRewardPlayer = null;
        private bool endPhase = false;

        public mainBoard()
        {
            InitializeComponent();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // save our current phase/trial data so we can output it
                CurrentPhase.Trials.Add(CurrentTrial);
                Phases.Add(CurrentPhase);

                // find our main window and call the outputData() function
                var windows = Application.Current.Windows.OfType<MainWindow>();

                if (windows.Count() > 0)
                {
                    var mainWindow = windows.First();
                    mainWindow.Phases = this.Phases;
                    mainWindow.outputData();
                }

                this.Close();
                MessageBox.Show("Experiment completed! Thank you for participating!");
            }
        }

        private void mainCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            initializeBoard();
            runPhase();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // clean up after ourselves :)
            mainCanvas.Children.RemoveRange(0, mainCanvas.Children.Count);
            mainTimer.Stop();
            rewardTimer.Stop();
            restTimer.Stop();
            phaseRestTimer.Stop();
            disposeObjects();
        }

        public void initializeBoard()
        {
            try
            {
                BaselineShapes = new List<Shape>();

                BaselineShapes.Add(new Shape(1, "circle.png"));

                TrialShapes = new List<Shape>();

                TrialShapes.Add(new Shape(1, "circle.png"));

                // set up our timers
                mainTimer.Interval = TimeSpan.FromSeconds(TrialDuration);
                restTimer.Interval = TimeSpan.FromSeconds(1); // need 1 second intervals to show a countdown timer
                phaseRestTimer.Interval = TimeSpan.FromSeconds(1);
                rewardTimer.Interval = TimeSpan.FromMilliseconds(200); // get the money label to blink a few times
                hitTimer.Interval = TimeSpan.FromMilliseconds(250);

                mainTimer.Tick += mainTimer_Tick;
                rewardTimer.Tick += rewardTimer_Tick;
                restTimer.Tick += restTimer_Tick;
                phaseRestTimer.Tick += phaseRestTimer_Tick;
                hitTimer.Tick += hitTimer_Tick;

                // initialize our sound players
                RewardPlayer = new SoundPlayer(Properties.Resources.CashRegister);
                RewardPlayer.Load();

                NoRewardPlayer = new SoundPlayer(Properties.Resources.toneHit);
                NoRewardPlayer.Load();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while initializing the board: " + e.Message);
                throw e;
            }
        }

        public bool runPhase()
        {
            try
            {
                endPhase = false;

                if (PhaseQueue.Count == 0 && Phases.Count == 0)
                {
                    MessageBox.Show("Invalid phase information received. Please try again.");
                    this.Close();
                    return false;
                }

                // End our experiment!!
                if (PhaseQueue.Count == 0 || Phases.Count == TotalPhasesValue)
                {
                    // find our main window and call the outputData() function
                    var windows = Application.Current.Windows.OfType<MainWindow>();

                    if (windows.Count() > 0)
                    {
                        var mainWindow = windows.First();
                        mainWindow.Phases = this.Phases;
                        mainWindow.outputData();
                    }

                    this.Close();
                    MessageBox.Show("Experiment completed! Thank you for participating!");
                    
                    return false;
                }

                CurrentPhase = PhaseQueue.Dequeue();

                ShapeA = BaselineShapes[0];

                BucketA = findBucket(ShapeA.ShapeID);

                runTrial();

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

        private void runTrial()
        {
            try
            {
                drawBoard();

                CurrentTrial = new Trial();
                CurrentTrialCount++;

                mainTimer.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while starting trial: " + e.Message);
                throw e;
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
                } else
                {
                    median = currCounts[(int)Math.Floor((decimal)(currCounts.Count / 2))];
                }

                variance = median * 0.20;

                foreach (int x in currCounts)
                {
                    if (x >= (median - variance) && x <= (median + variance))
                    {
                        isStable = true;
                    } else
                    {
                        isStable = false;
                        break;
                    }
                }

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
                    yLogs.Add(Math.Round(Math.Log10(i), 2));
                }

                // Calculate sigma XY
                double sigXY = 0.0;

                for (int i = 0; i < 5; i++)
                {
                    sigXY += Math.Round((x[i] * yLogs[i]), 2);
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
                double slope = Math.Round(nSigXY - (sumX * sumYLogs), 2);

                // Calculate N sigma X squared
                int nSigXSquared = 0;

                foreach (int i in x)
                {
                    nSigXSquared += (i * i);
                }

                nSigXSquared = 5 * nSigXSquared;

                // Calculate the final slope value
                slope = Math.Round(slope / (nSigXSquared - (sumX * sumX)), 2);

                // Now onto the Y intercept
                double yIntercept = Math.Round((sumYLogs - (slope * sumX)) / 5, 2);

                // Second step: Calculate the actual celeration value
                // Will be using positive numbers for x VALUE celeration,
                // negative numbers for % VALUE celeration for convenience sake
                double yVal = Math.Round(slope * x[0] + yIntercept, 2);
                double startFreq = Math.Round(Math.Pow(10, yVal), 2);

                yVal = Math.Round(slope * x[5 - 1] + yIntercept, 2);
                double endFreq = Math.Round(Math.Pow(10, yVal), 2);

                double celerationVal = Math.Round(Math.Pow(endFreq / startFreq, 0.2), 2);

                celerationVal = Math.Round(Math.Pow(celerationVal, 7), 2);

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

        public bool drawBoard()
        {
            Image newImage;

            int xLoc = 0;
            int yLoc = 0;

            try
            {
                // suspend the layout while we're adding the new controls
                using (var d = Dispatcher.DisableProcessing())
                {
                    // dispose of any controls that we already placed
                    if (ImageSet.Count > 0)
                    {
                        foreach(Image i in ImageSet)
                        {
                            mainCanvas.Children.Remove(i);
                        }
                    }

                    // 11/30/2015 Only one bucket and one circle shape, set them 300px apart
                    // Place the bucket first
                    xLoc = (int)(mainCanvas.ActualWidth / 2) - 150 - 30;
                    yLoc = (int)(mainCanvas.ActualHeight / 2) - 30;

                    BucketA.Location = new Point(xLoc, yLoc);

                    newImage = new Image();
                    newImage.Source = new BitmapImage(BucketA.ImagePath);
                    newImage.Tag = BucketA;
                    newImage.Width = newImage.Source.Width;
                    newImage.Height = newImage.Source.Height;

                    mainCanvas.Background = new SolidColorBrush(CurrentPhase.BackgroundColor);
                    mainCanvas.Children.Add(newImage);
                    ImageSet.Add(newImage);
                    Canvas.SetTop(newImage, yLoc);
                    Canvas.SetLeft(newImage, xLoc);

                    // Now place the circle shape
                    xLoc = (int)(mainCanvas.ActualWidth / 2) + 150 - 30;
                    yLoc = (int)(mainCanvas.ActualHeight / 2) - 30;

                    newImage = new Image();
                    newImage.Source = new BitmapImage(ShapeA.ImagePath);
                    newImage.Tag = ShapeA;
                    newImage.Width = newImage.Source.Width;
                    newImage.Height = newImage.Source.Height;

                    mainCanvas.Background = new SolidColorBrush(CurrentPhase.BackgroundColor);
                    mainCanvas.Children.Add(newImage);
                    ImageSet.Add(newImage);
                    Canvas.SetTop(newImage, yLoc);
                    Canvas.SetLeft(newImage, xLoc);

                    moneyLB.Content = this.MoneyValue.ToString("C2");
                    Canvas.SetLeft(moneyLB, mainCanvas.ActualWidth / 2 - moneyLB.ActualWidth / 2);
                }
                
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while drawing board: " + e.Message);
                this.Close();
                return false;
                throw e;
            }
        }

        private Shape findBucket(int id)
        {
            string filePath = "";

            filePath = "bucket-empty.png";

            return new Shape(id, filePath, true);
        }       

        private void mainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Object temp = e.Source;
            Type type = temp.GetType();
            Shape shapeInfo = null;

            // check if we're even clicking on a valid control
            if (!typeof(Image).IsAssignableFrom(type)) return;

            var image = e.Source as Image;
            
            if (image.Tag == null) return;

            shapeInfo = (Shape)image.Tag;
            
            if (shapeInfo.IsBucket) return;

            if (image != null && mainCanvas.CaptureMouse())
            {
                mousePosition = e.GetPosition(mainCanvas);
                draggedImage = image;
                Panel.SetZIndex(draggedImage, 1); // in case of multiple images
            }
        }

        private void mainCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var xLoc = 0;
            var yLoc = 0;

            if (draggedImage != null)
            {
                Shape shapeInfo = (Shape)draggedImage.Tag;

                if (shapeInfo.IsBucket) return;

                mousePosition = e.GetPosition(mainCanvas);

                if (checkBucket(mousePosition, shapeInfo.ShapeID))
                {
                    // 11/30/2015 set a delay, and then show the shape!
                    mainCanvas.Children.Remove(draggedImage);
                    
                    hitTimer.Start();                  
                } else
                {
                    // return this image to where it was originally
                    xLoc = (int)(mainCanvas.ActualWidth / 2) + 150 - 30;
                    yLoc = (int)(mainCanvas.ActualHeight / 2) - 30;

                    Canvas.SetLeft(draggedImage, xLoc);
                    Canvas.SetTop(draggedImage, yLoc);

                    // 10/26/15 aaaand count it as a miss
                    CurrentTrial.MissCount++;
                }

                mainCanvas.ReleaseMouseCapture();
                Panel.SetZIndex(draggedImage, 0);
                draggedImage = null;
            }
        }

        private void mainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (draggedImage != null)
            {
                Shape shapeInfo = (Shape)draggedImage.Tag;

                if (shapeInfo.IsBucket) return;

                var position = e.GetPosition(mainCanvas);
                var offset = position - mousePosition;
                mousePosition = position;
                Canvas.SetLeft(draggedImage, Canvas.GetLeft(draggedImage) + offset.X);
                Canvas.SetTop(draggedImage, Canvas.GetTop(draggedImage) + offset.Y);
            }
        }

        private bool checkBucket(Point point, int id)
        {
            try
            {
                // Check if our point is within a bucket's boundaries (do we need padding?)
                // If not a hit, then get of here!
                // If yes, check if we have the right bucket and shape pairing
                // If we do, then success++ else miss++
                if (point.X >= BucketA.Location.X 
                    && point.X <= (BucketA.Location.X + Constants.BucketWidth)
                    && point.Y >= BucketA.Location.Y 
                    && point.Y <= (BucketA.Location.Y + Constants.BucketHeight))
                {
                    if (id == BucketA.ShapeID)
                    {
                        CurrentTrial.SuccessCount++;
                    }
                    else
                    {
                        CurrentTrial.MissCount++;
                    }
                    
                    return true;
                }

                //if (point.X >= BucketB.Location.X
                //    && point.X <= (BucketB.Location.X + Constants.BucketWidth)
                //    && point.Y >= BucketB.Location.Y
                //    && point.Y <= (BucketB.Location.Y + Constants.BucketHeight))
                //{
                //    if (id == BucketB.ShapeID)
                //    {
                //        CurrentTrial.SuccessCount++;
                //    }
                //    else
                //    {
                //        CurrentTrial.MissCount++;
                //    }
                //    return true;
                //}

                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while checking for valid bucket: " + e.Message);
                return false;
                throw e;
            }
        }

        private void mainTimer_Tick(object sender, EventArgs e)
        {
            // Trial ended, make sure to clear our mouse so we're not picking up any pieces
            // Do calculations, add to our trial list and continue
            mainTimer.Stop();
            clearMouseActions();
            calculateResponse();

            // start our reward timer so we can "flicker" the money amount
            // also play our reward sound!        
            toggleRewardTimer(true);

            if (RewardOn)
            {
                RewardPlayer.Play();
            } else
            {
                NoRewardPlayer.Play();
            }
        }

        private void calculateResponse()
        {
            List<int> successValues = null;
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

                if (CurrentPhase.Label == Constants.PhaseBaseline)
                {
                    ObservationList.Add(CurrentTrial.SuccessCount);
                    CurrentPhase.Trials.Add(CurrentTrial);
                    RewardOn = false;
                } else
                {
                    responseIndex = (int)Math.Floor((CurrentPhase.Observations + 1) * (1 - CurrentPhase.Density));

                    successValues = (from o in ObservationList
                                     orderby o ascending
                                     select o).ToList();

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

                    // add our latest observation value so we can use it for subsequent trials
                    // but keep the list count at our main m value
                    if (ObservationList.Count == CurrentPhase.Observations)
                    {
                        ObservationList.RemoveAt(0);
                    }

                    ObservationList.Add(CurrentTrial.SuccessCount);

                    // save our current trial data and add it to our current phase's trial list
                    CurrentTrial.ResponseValue = successValues[responseIndex - 1];
                    CurrentTrial.Money = MoneyValue;
                    CurrentPhase.Trials.Add(CurrentTrial);

                    // update our money label
                    moneyLB.Content = MoneyValue.ToString("C");
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
                    if (CurrentPhase.Label == 'A')
                    {
                        celerationVal = calculateCelerationValue();

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
                    }
                    else if (CurrentPhase.Label == 'B')
                    {
                        celerationVal = calculateCelerationValue();

                        if ((celerationVal < 0 && checkStability()) || checkZeroSum())
                        {
                            endPhase = true;

                            CurrentPhase.CelerationValue = celerationVal;

                            PhaseQueue.Enqueue(new Phase(PhaseCTemplate));
                        }
                    }
                    else if (CurrentPhase.Label == 'C')
                    {
                        celerationVal = calculateCelerationValue();

                        if ((celerationVal > 0 && checkStability()) || checkZeroSum())
                        {
                            endPhase = true;

                            CurrentPhase.CelerationValue = celerationVal;

                            PhaseQueue.Enqueue(new Phase(PhaseBTemplate));
                        }
                    }

                    celerationLB.Content = celerationVal;

                    // Decide if we're going to end our phase
                    if (endPhase || CurrentTrialCount >= MaxTrialValue)
                    {
                        CurrentTrialCount = 0;
                        Phases.Add(CurrentPhase);
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

        private void rewardTimer_Tick(object sender, EventArgs e)
        {
            // let the money blink a few times, then kick them over to the rest phase
            CurrentMillis -= rewardTimer.Interval.Milliseconds;

            if (CurrentMillis <= 0)
            {
                moneyLB.Foreground = Brushes.Black;
                toggleRewardTimer(false);

                // decide whether or not we're going to show our rest or our phase rest screen
                if (endPhase)
                {
                    togglePhaseRestTimer(true);
                } else
                {
                    toggleRestTimer(true);
                }
            }
            else
            {
                if (BlinkOn) moneyLB.Foreground = Brushes.White;
                else moneyLB.Foreground = Brushes.Black;
                BlinkOn = !BlinkOn;
            }         
        }

        private void hitTimer_Tick(object sender, EventArgs e)
        {
            var xLoc = 0;
            var yLoc = 0;
            Image newImage;

            hitTimer.Stop();

            xLoc = (int)(mainCanvas.ActualWidth / 2) + 150 - 30;
            yLoc = (int)(mainCanvas.ActualHeight / 2) - 30;

            newImage = new Image();
            newImage.Source = new BitmapImage(ShapeA.ImagePath);
            newImage.Tag = ShapeA;
            newImage.Width = newImage.Source.Width;
            newImage.Height = newImage.Source.Height;

            mainCanvas.Children.Add(newImage);
            ImageSet.Add(newImage);
            Canvas.SetTop(newImage, yLoc);
            Canvas.SetLeft(newImage, xLoc);
        }

        private void toggleRewardTimer(bool on)
        {
            if (on)
            {
                toggleRestCanvas(true);
                CurrentMillis = 2000;
                rewardTimer.Start();
            }
            else
            {
                rewardTimer.Stop();
            }
        }

        private void toggleRestTimer(bool on)
        {
            if (on)
            {
                toggleRestCanvas(true);
                CurrentMillis = TrialRestDuration * 1000;
                countDownLB.Visibility = Visibility.Visible;
                Canvas.SetLeft(countDownLB, restCanvas.ActualWidth / 2 - countDownLB.ActualWidth / 2);
                Canvas.SetTop(countDownLB, restCanvas.ActualHeight / 2 - countDownLB.ActualHeight / 2);
                restTimer.Start();
            }
            else
            {
                restTimer.Stop();
                countDownLB.Visibility = Visibility.Hidden;
                toggleRestCanvas(false);
            }           
        }

        private void togglePhaseRestTimer(bool on)
        {
            if (on)
            {
                toggleRestCanvas(true);
                CurrentMillis = PhaseRestDuration * 1000;
                countDownLB.Visibility = Visibility.Visible;
                countDownLB.Content = PhaseRestDuration;
                Canvas.SetLeft(countDownLB, restCanvas.ActualWidth / 2 - countDownLB.ActualWidth / 2);
                Canvas.SetTop(countDownLB, restCanvas.ActualHeight / 2 - countDownLB.ActualHeight / 2);
                phaseRestTimer.Start();
            }
            else
            {
                phaseRestTimer.Stop();
                countDownLB.Visibility = Visibility.Hidden;
                toggleRestCanvas(false);
            }
        }

        private void restTimer_Tick(object sender, EventArgs e)
        {
            CurrentMillis -= restTimer.Interval.Seconds * 1000;

            if (CurrentMillis <= 0)
            {
                // stop our resting timer and start the next trial
                toggleRestTimer(false);
                runTrial();
            }

            countDownLB.Content = CurrentMillis / 1000;
        }

        private void toggleRestCanvas(bool on)
        {
            if (on)
            {
                restCanvas.Visibility = Visibility.Visible;
                restCanvas.Width = mainCanvas.ActualWidth;
                restCanvas.Height = mainCanvas.ActualHeight;

                countDownLB.Content = TrialRestDuration;
            }
            else
            {
                restCanvas.Visibility = Visibility.Hidden;
            }
        }

        private void clearMouseActions()
        {
            mainCanvas.ReleaseMouseCapture();

            if (draggedImage != null)
            {
                Panel.SetZIndex(draggedImage, 0);
                draggedImage = null;
            }          
        }

        private void phaseRestTimer_Tick(object sender, EventArgs e)
        {
            CurrentMillis -= phaseRestTimer.Interval.Seconds * 1000;

            if (CurrentMillis <= 0)
            {
                // stop our resting timer and call runTrial()
                // runTrial() will decide if we go to the next phase or not
                togglePhaseRestTimer(false);
                runPhase();
            }

            countDownLB.Content = CurrentMillis / 1000;
        }

        private void disposeObjects()
        {
            Phases = null;
            PhaseQueue = null;
            SkeletonBoard = null;
            BaselineShapes = null;
            TrialShapes = null;
            ImageSet = null;
            ObservationList = null;
            mainTimer = null;
            restTimer = null;
            phaseRestTimer = null;
            rewardTimer = null;
            hitTimer = null;
            RewardPlayer = null;
            NoRewardPlayer = null;
        }
    }
}
