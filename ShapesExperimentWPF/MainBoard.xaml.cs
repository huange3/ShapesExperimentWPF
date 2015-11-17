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

namespace ShapesExperimentWPF
{
    /// <summary>
    /// Interaction logic for mainBoard.xaml
    /// </summary>
    public partial class mainBoard : Window
    {
        public List<Phase> Phases = new List<Phase>();
        public Queue<Phase> PhaseQueue = null;
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
        public int MainObservationCount = 0;
        public int TrialDuration = 0;
        public int TrialRestDuration = 0;
        public int PhaseRestDuration = 0;
        public decimal MoneyValue;
        public decimal RewardValue;
        private int CurrentTrialCount = 0;
        private int CurrentMillis = 0;      
        private bool BlinkOn = true;
        private bool RewardOn = false;

        static Random rand = new Random();
        private DispatcherTimer mainTimer = new DispatcherTimer();
        private DispatcherTimer restTimer = new DispatcherTimer();
        private DispatcherTimer phaseRestTimer = new DispatcherTimer();
        private DispatcherTimer rewardTimer = new DispatcherTimer();

        private Image draggedImage;
        private Point mousePosition;
        private Point startMousePosition;
        private Sounds soundPlayer;

        public mainBoard()
        {
            InitializeComponent();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
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

                BaselineShapes.Add(new Shape(1, "heart-01.png"));
                BaselineShapes.Add(new Shape(2, "diamond-01.png"));

                TrialShapes = new List<Shape>();

                TrialShapes.Add(new Shape(3, "sun-02.png"));
                TrialShapes.Add(new Shape(4, "moon-02.png"));

                // create ourselves a board that we can shuffle to drive 
                // the drawBoard() function
                // X & Y are buckets
                // A is shape #1, B is shape #2
                for (var i = 0; i < 64; i++)
                {
                    if (i == 0) SkeletonBoard.Add(Constants.BucketA);

                    else if (i == 1) SkeletonBoard.Add(Constants.BucketB);

                    else if (i >= 2 && i <= 32) SkeletonBoard.Add(Constants.ShapeA);

                    else SkeletonBoard.Add(Constants.ShapeB);
                }

                // set up our timers
                mainTimer.Interval = TimeSpan.FromSeconds(TrialDuration);
                restTimer.Interval = TimeSpan.FromSeconds(1); // need 1 second intervals to show a countdown timer
                phaseRestTimer.Interval = TimeSpan.FromSeconds(1);
                rewardTimer.Interval = TimeSpan.FromMilliseconds(200); // get the money label to blink a few times

                mainTimer.Tick += mainTimer_Tick;
                rewardTimer.Tick += rewardTimer_Tick;
                restTimer.Tick += restTimer_Tick;
                phaseRestTimer.Tick += phaseRestTimer_Tick;

                // initialize our sound player
                soundPlayer = new Sounds();
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
                if (PhaseQueue.Count == 0 && Phases.Count == 0)
                {
                    MessageBox.Show("Invalid phase information received. Please try again.");
                    this.Close();
                    return false;
                }

                if (PhaseQueue.Count == 0)
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

                if (CurrentPhase.Label == Constants.PhaseBaseline)
                {
                    ShapeA = BaselineShapes[0];
                    ShapeB = BaselineShapes[1];
                }
                else if (CurrentPhase.Label == Constants.PhaseB)
                {
                    ShapeA = TrialShapes[0];
                    ShapeB = TrialShapes[1];
                }
                else if (CurrentPhase.Label == Constants.PhaseC)
                {
                    ShapeA = TrialShapes[0];
                    ShapeB = TrialShapes[1];
                }

                // 10/26/15 Only one bucket now, bucket shape dependent on phase
                BucketA = findBucket(ShapeA.ShapeID);
                BucketB = findBucket(ShapeB.ShapeID);

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
                // If we've already hit our trial limit, then kick them over to the next phase.
                if (CurrentTrialCount == CurrentPhase.TrialCount)
                {
                    CurrentTrialCount = 0;
                    Phases.Add(CurrentPhase);
                    runPhase();
                }
                else
                {
                    drawBoard();

                    CurrentTrial = new Trial();
                    CurrentTrialCount++;

                    mainTimer.Start();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while starting trial: " + e.Message);
                throw e;
            }
        }

        public Boolean drawBoard()
        {
            Uri currImage = null;
            Object currTag = null;
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

                    // shuffle our skeleton board and then draw the controls
                    // onto the board
                    SkeletonBoard.Shuffle();

                    for (var i = 0; i < SkeletonBoard.Count; i++)
                    {
                        if (i == 0)
                        {
                            xLoc = (int)mainCanvas.ActualWidth / 2 - 65 * 4;
                            yLoc = (int)mainCanvas.ActualHeight / 2 - 64 * 4;
                        }
                        else if (i > 0 && i % 8 == 0)
                        {
                            xLoc = (int)mainCanvas.ActualWidth / 2 - 65 * 4; ;
                            yLoc += 65;
                        }
                        else if (i > 0)
                        {
                            xLoc += 65;
                        }

                        // if these shapes are buckets, remember to save their locations
                        // assign tags to all objects so we can check their shape IDs
                        switch (SkeletonBoard[i])
                        {
                            case Constants.BucketA:
                                currImage = BucketA.ImagePath;
                                BucketA.Location = new Point(xLoc, yLoc);
                                currTag = BucketA;
                                break;
                            case Constants.BucketB:
                                currImage = BucketB.ImagePath;                               
                                BucketB.Location = new Point(xLoc, yLoc);
                                currTag = BucketB;
                                break;
                            case Constants.ShapeA:
                                currImage = ShapeA.ImagePath;
                                currTag = ShapeA;
                                break;
                            case Constants.ShapeB:
                                currImage = ShapeB.ImagePath;
                                currTag = ShapeB;
                                break;
                            default:
                                break;
                        }

                        // create our new image control and add it to the main canvas
                        newImage = new Image();
                        newImage.Source = new BitmapImage(currImage);
                        newImage.Tag = currTag;
                        newImage.Width = newImage.Source.Width;
                        newImage.Height = newImage.Source.Height;

                        mainCanvas.Background = new SolidColorBrush(CurrentPhase.BackgroundColor);
                        mainCanvas.Children.Add(newImage);
                        ImageSet.Add(newImage);
                        Canvas.SetTop(newImage, yLoc);
                        Canvas.SetLeft(newImage, xLoc);
                    }

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

            //switch (id)
            //{
            //    case 1:
            //        filePath = "bucket-heart.png";
            //        break;
            //    case 2:
            //        filePath = "bucket-diamond.png";
            //        break;
            //    case 3:
            //        filePath = "bucket-sun.png";
            //        break;
            //    case 4:
            //        filePath = "bucket-moon.png";
            //        break;
            //    default:
            //        filePath = "";
            //        break;
            //}
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
                mousePosition = startMousePosition = e.GetPosition(mainCanvas);
                draggedImage = image;
                Panel.SetZIndex(draggedImage, 1); // in case of multiple images
            }
        }

        private void mainCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (draggedImage != null)
            {
                Shape shapeInfo = (Shape)draggedImage.Tag;

                if (shapeInfo.IsBucket) return;

                mousePosition = e.GetPosition(mainCanvas);

                if (checkBucket(mousePosition, shapeInfo.ShapeID))
                {
                    mainCanvas.Children.Remove(draggedImage);
                } else
                {
                    // return this image to where it was originally
                    Canvas.SetLeft(draggedImage, startMousePosition.X - 30);
                    Canvas.SetTop(draggedImage, startMousePosition.Y - 30);

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

                if (point.X >= BucketB.Location.X
                    && point.X <= (BucketB.Location.X + Constants.BucketWidth)
                    && point.Y >= BucketB.Location.Y
                    && point.Y <= (BucketB.Location.Y + Constants.BucketHeight))
                {
                    if (id == BucketB.ShapeID)
                    {
                        CurrentTrial.SuccessCount++;
                    }
                    else
                    {
                        CurrentTrial.MissCount++;
                    }
                    return true;
                }

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

            if (RewardOn) soundPlayer.playReward();
            else soundPlayer.playNoReward();
        }

        private void calculateResponse()
        {
            List<int> successValues = null;
            int responseIndex = 0;

            try
            {
                // If this is the baseline phase, then skip calculations and just add to list!
                // Otherwise, get our observations from the baseline phase
                // Sort the success values according to the phase rank
                // Get our response index from the current phase info
                // Reward the user if our current success count satisfies the rank criteria
                // either greater than or less than the response index
                if (CurrentPhase.Label == Constants.PhaseBaseline)
                {
                    ObservationList.Add(CurrentTrial.SuccessCount);
                    CurrentPhase.Trials.Add(CurrentTrial);
                    return;
                }

                responseIndex = (int)Math.Floor((MainObservationCount + 1) * (1 - CurrentPhase.Density));

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

                if (CurrentPhase.RankType == Constants.GreaterThan)
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
                if (ObservationList.Count == MainObservationCount)
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
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while calculating response value: " + e.Message);
                throw e;
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
                if (CurrentTrialCount == CurrentPhase.TrialCount)
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
                runTrial();
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
            soundPlayer = null;
        }
    }
}
