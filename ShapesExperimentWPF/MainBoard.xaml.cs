﻿using System;
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
using System.Drawing;
using System.Windows.Threading;
using System.IO;

namespace ShapesExperimentWPF
{
    /// <summary>
    /// Interaction logic for mainBoard.xaml
    /// </summary>
    public partial class mainBoard : Window
    {
        public List<Phase> Phases = null;
        public Queue<Phase> PhaseQueue = null;
        private Phase CurrentPhase = null;
        private Trial CurrentTrial = null;
        private List<Shape> BaselineShapes = null;
        private List<Shape> TrialShapes = null;
        private List<char> SkeletonBoard = new List<char>(64);
        private Shape ShapeA = null;
        private Shape ShapeB = null;
        private Shape BucketA = null;
        private Shape BucketB = null;
        private List<int> ObservationList = new List<int>();

        public int TrialDuration = 0;
        public int TrialRestDuration = 0;
        public decimal MoneyValue;
        public decimal RewardValue;
        private int CurrentTrialCount = 0;
        private int CurrentMillis = 0;
        private bool BlinkOn = false;

        static Random rand = new Random();
        private DispatcherTimer mainTimer = new DispatcherTimer();
        private DispatcherTimer restTimer = new DispatcherTimer();
        private DispatcherTimer rewardTimer = new DispatcherTimer();

        private System.Windows.Controls.Image draggedImage;
        private System.Windows.Point mousePosition;
        private System.Windows.Point startMousePosition;

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

        public void initializeBoard()
        {
            try
            {
                BaselineShapes = new List<Shape>();

                BaselineShapes.Add(new Shape(1, "gray-01.png"));
                BaselineShapes.Add(new Shape(2, "gray-02.png"));
                BaselineShapes.Add(new Shape(3, "gray-03.png"));
                BaselineShapes.Add(new Shape(4, "gray-04.png"));
                BaselineShapes.Add(new Shape(5, "gray-05.png"));
                BaselineShapes.Add(new Shape(6, "gray-06.png"));

                TrialShapes = new List<Shape>();

                TrialShapes.Add(new Shape(1, "blue-01.png"));
                TrialShapes.Add(new Shape(2, "blue-02.png"));
                TrialShapes.Add(new Shape(3, "blue-03.png"));
                TrialShapes.Add(new Shape(4, "blue-04.png"));
                TrialShapes.Add(new Shape(5, "blue-05.png"));
                TrialShapes.Add(new Shape(6, "blue-06.png"));

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
                mainTimer.Interval = new TimeSpan(TrialDuration * 1000);
                restTimer.Interval = new TimeSpan(1000); // need 1 second intervals to show a countdown timer
                rewardTimer.Interval = new TimeSpan(400); // get the money label to blink a few times

                mainTimer.Tick += mainTimer_Tick;
                rewardTimer.Tick += rewardTimer_Tick;
                restTimer.Tick += restTimer_Tick;
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
                    MessageBox.Show("Experiment completed! Thank you for participating!");
                    outputData();
                    this.Close();
                    return false;
                }

                /* MODUS OPERANDI
                ------------------
                1. Dequeue our first phase
                2. Check the phase type (special shapes for baseline)
                3. Draw our board
                4. Start the timer*/

                CurrentPhase = PhaseQueue.Dequeue();

                // pick our two random shapes by shuffling our shape lists and taking the first two shapes
                if (CurrentPhase.Label == Constants.PhaseBaseline)
                {
                    BaselineShapes.Shuffle();
                    ShapeA = BaselineShapes[0];
                    ShapeB = BaselineShapes[1];
                }
                else
                {
                    TrialShapes.Shuffle();
                    ShapeA = TrialShapes[0];
                    ShapeB = TrialShapes[1];
                }

                // find our respective buckets
                BucketA = findBucket(ShapeA.ShapeID);
                BucketB = findBucket(ShapeB.ShapeID);

                if (drawBoard()) runTrial();

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

        public Boolean drawBoard()
        {
            Uri currImage = null;
            Object currTag = null;
            System.Windows.Controls.Image newImage;

            int xLoc = 0;
            int yLoc = 0;

            try
            {
                // suspend the layout while we're adding the new controls
                using (var d = Dispatcher.DisableProcessing())
                {
                    // shuffle our skeleton board and then draw the controls
                    // onto the board
                    SkeletonBoard.Shuffle();

                    for (var i = 0; i < SkeletonBoard.Count; i++)
                    {
                        if (i == 0)
                        {
                            xLoc = (int)mainCanvas.ActualWidth / 2 - (65 * 4);
                            yLoc = (int)mainCanvas.ActualHeight / 2 - (65 * 4);
                        }
                        else if (i > 0 && i % 8 == 0)
                        {
                            xLoc = (int)mainCanvas.ActualWidth / 2 - (65 * 4);
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
                                BucketA.Location = new System.Windows.Point(xLoc, yLoc);
                                currTag = BucketA;
                                break;
                            case Constants.BucketB:
                                currImage = BucketB.ImagePath;                               
                                BucketB.Location = new System.Windows.Point(xLoc, yLoc);
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
                        newImage = new System.Windows.Controls.Image();
                        newImage.Source = new BitmapImage(currImage);
                        newImage.Tag = currTag;
                        newImage.Width = newImage.Source.Width;
                        newImage.Height = newImage.Source.Height;

                        mainCanvas.Background = new SolidColorBrush(CurrentPhase.BackgroundColor);
                        mainCanvas.Children.Add(newImage);
                        Canvas.SetTop(newImage, yLoc);
                        Canvas.SetLeft(newImage, xLoc);
                    }

                    moneyLB.Content = this.MoneyValue.ToString("C");
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

        private void runTrial()
        {
            try
            {
                // If we've already hit our trial limit, then kick them over to the next phase.
                if (CurrentTrialCount == CurrentPhase.Observations)
                {
                    runPhase();
                    return;
                }

                CurrentTrial = new Trial();
                CurrentTrialCount++;

                successCountLB.Content = "0";
                missCountLB.Content = "0";

                mainTimer.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show("Error occurred while starting trial: " + e.Message);
                throw e;
            }
        }

        private Shape findBucket(int id)
        {
            string filePath = "";

            switch (id)
            {
                case 1:
                    filePath = "bucket-01.png";
                    break;
                case 2:
                    filePath = "bucket-02.png";
                    break;
                case 3:
                    filePath = "bucket-03.png";
                    break;
                case 4:
                    filePath = "bucket-04.png";
                    break;
                case 5:
                    filePath = "bucket-05.png";
                    break;
                case 6:
                    filePath = "bucket-06.png";
                    break;
                default:
                    filePath = "";
                    break;
            }

            return new Shape(id, filePath, true);
        }       

        public void outputData()
        {

        }

        private void mainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var image = e.Source as System.Windows.Controls.Image;
            Shape shapeInfo = (Shape)image.Tag;

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
                    Canvas.SetLeft(draggedImage, startMousePosition.X);
                    Canvas.SetTop(draggedImage, startMousePosition.Y);
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

        private bool checkBucket(System.Windows.Point point, int id)
        {
            try
            {
                // Check if our point is within a bucket's boundaries (do we need padding?)
                // If no, then get of here!
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
                        successCountLB.Content = CurrentTrial.SuccessCount;
                    }
                    else
                    {
                        CurrentTrial.MissCount++;
                        missCountLB.Content = CurrentTrial.MissCount;
                    }
                    
                    return true;
                }

                if (point.X >= BucketB.Location.X
                    && point.X <= (BucketB.Location.X + Constants.BucketWidth)
                    && point.Y >= BucketB.Location.Y
                    && point.Y <= (BucketB.Location.Y + Constants.BucketHeight))
                {
                    if (id == BucketB.ShapeID) {
                        CurrentTrial.SuccessCount++;
                        successCountLB.Content = CurrentTrial.SuccessCount;
                    } 
                    else
                    {
                        CurrentTrial.MissCount++;
                        missCountLB.Content = CurrentTrial.MissCount;
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // clean up after ourselves :)
            mainCanvas.Children.RemoveRange(0, mainCanvas.Children.Count);
        }

        private void mainTimer_Tick(object sender, EventArgs e)
        {
            calculateResponse();
            // start our reward timer so we can "flicker" the money amount
            CurrentMillis = 2000;
            rewardTimer.Start();
        }

        private void calculateResponse()
        {
            List<int> successValues = null;
            int responseIndex = 0;

            try
            {
                // If this is the baseline phase, then skip!
                // Otherwise, get our observations from the baseline phase
                // Sort the success values according to the phase rank
                // Get our response index from the current phase info
                // Reward the user if our current success count satisfies the rank criteria
                // either greater than or less than the response index
                if (CurrentPhase.Label == Constants.PhaseBaseline) return;

                // check if we have an observation list to compare to
                // if not, load it from the baseline phase
                if (ObservationList.Count == 0)
                {
                    ObservationList = (from t in Phases[0].Trials
                                       select t.SuccessCount).ToList();
                }

                responseIndex = CurrentPhase.ResponseIndex;

                successValues = (from o in ObservationList
                                 orderby o ascending
                                 select o).ToList();

                if (CurrentPhase.RankType == Constants.LessThan)
                {
                    if (CurrentTrial.SuccessCount < successValues[responseIndex])
                    {
                        MoneyValue += RewardValue;
                    }
                }

                if (CurrentPhase.RankType == Constants.GreaterThan)
                {
                    if (CurrentTrial.SuccessCount > successValues[responseIndex])
                    {
                        MoneyValue += RewardValue;
                    }
                }

                // add our latest observation value so we can use it for subsequent trials
                // but keep the list count at our m value
                ObservationList.RemoveAt(0);
                ObservationList.Add(CurrentTrial.SuccessCount);

                // save our current trial data and add it to our current phase's trial list
                CurrentTrial.ResponseValue = successValues[responseIndex];
                CurrentTrial.Money = MoneyValue;
                CurrentPhase.Trials.Add(CurrentTrial);
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
                rewardTimer.Stop();
                toggleRestTimer(true);
                return;
            }

            if (BlinkOn) moneyLB.Content = "";
            else moneyLB.Content = MoneyValue;
            BlinkOn = !BlinkOn;
        }

        private void toggleRestTimer(bool on)
        {
            if (on)
            {
                CurrentMillis = TrialRestDuration * 1000;
                // show our resting background and countdown label
                // TODO
                restTimer.Start();
            }
            else
            {
                restTimer.Stop();
                // hide our resting background
                // bring our main canvas back up to the front
                // redraw the board, start next trial
                // TODO
            }
            
        }

        private void restTimer_Tick(object sender, EventArgs e)
        {
            CurrentMillis -= restTimer.Interval.Milliseconds;

            if (CurrentMillis <= 0)
            {
                toggleRestTimer(false);
            }
        }
    }
}
