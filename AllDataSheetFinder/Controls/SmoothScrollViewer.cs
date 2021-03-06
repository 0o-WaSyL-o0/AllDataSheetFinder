﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AllDataSheetFinder.Controls
{
    public class SmoothScrollViewer : ScrollViewer
    {
        public SmoothScrollViewer()
        {
            m_animation = new DoubleAnimation();

            m_endingAnimation = new DoubleAnimation();
            m_endingAnimation.DecelerationRatio = 0.8;

            m_animation.Duration = new Duration(TimeSpan.FromMilliseconds(100));
            m_endingAnimation.BeginTime = m_animation.Duration.TimeSpan;
            
            m_endingAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(150));          

            m_storyboard = new Storyboard();
            m_storyboard.Children.Add(m_animation);
            m_storyboard.Children.Add(m_endingAnimation);
            Storyboard.SetTarget(m_animation, this);
            Storyboard.SetTarget(m_endingAnimation, this);
            Storyboard.SetTargetProperty(m_animation, new PropertyPath(SmoothScrollViewer.OffsetProperty));
            Storyboard.SetTargetProperty(m_endingAnimation, new PropertyPath(SmoothScrollViewer.OffsetProperty));
        }

        private DoubleAnimation m_animation;
        private DoubleAnimation m_endingAnimation;
        private Storyboard m_storyboard;
        private bool m_storyboardBegan = false;

        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register("Offset", typeof(double), typeof(SmoothScrollViewer), new PropertyMetadata(OnOffsetChanged));
        public double Offset
        {
            get { return (double)GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }
        
        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register("Duration", typeof(TimeSpan), typeof(SmoothScrollViewer), new PropertyMetadata(OnDurationChanged));
        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }
        
        public static readonly DependencyProperty SlowdownDurationProperty = DependencyProperty.Register("SlowdownDuration", typeof(TimeSpan), typeof(SmoothScrollViewer), new PropertyMetadata(OnSlowdownDurationChanged));
        public TimeSpan SlowdownDuration
        {
            get { return (TimeSpan)GetValue(SlowdownDurationProperty); }
            set { SetValue(SlowdownDurationProperty, value); }
        }

        public static readonly DependencyProperty MouseWheelDeltaDividerProperty = DependencyProperty.Register("MouseWheelDeltaDivider", typeof(double), typeof(SmoothScrollViewer), new PropertyMetadata(1.0));
        public double MouseWheelDeltaDivider
        {
            get { return (double)GetValue(MouseWheelDeltaDividerProperty); }
            set { SetValue(MouseWheelDeltaDividerProperty, value); }
        }

        public static readonly DependencyProperty IsSmoothScrollingEnabledProperty = DependencyProperty.Register("IsSmoothScrollingEnabled", typeof(bool), typeof(SmoothScrollViewer), new PropertyMetadata(true));
        public bool IsSmoothScrollingEnabled
        {
            get { return (bool)GetValue(IsSmoothScrollingEnabledProperty); }
            set { SetValue(IsSmoothScrollingEnabledProperty, value); }
        }

        private static void OnOffsetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            SmoothScrollViewer viewer = sender as SmoothScrollViewer;
            viewer.ScrollToVerticalOffset((double)e.NewValue);
        }
        private static void OnDurationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            SmoothScrollViewer viewer = sender as SmoothScrollViewer;
            viewer.m_animation.Duration = new Duration((TimeSpan)e.NewValue);
            viewer.m_endingAnimation.BeginTime = viewer.m_animation.Duration.TimeSpan;
        }
        private static void OnSlowdownDurationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            SmoothScrollViewer viewer = sender as SmoothScrollViewer;
            viewer.m_endingAnimation.Duration = new Duration((TimeSpan)e.NewValue);
        }

        protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!IsSmoothScrollingEnabled)
            {
                base.OnMouseWheel(e);
                return;
            }

            e.Handled = true;

            double delta = -e.Delta / MouseWheelDeltaDivider;

            TimeSpan storyboardTime = (m_storyboardBegan ? m_storyboard.GetCurrentTime() : TimeSpan.Zero);
            if (storyboardTime > TimeSpan.Zero && storyboardTime < Duration + SlowdownDuration)
            {
                m_storyboard.Stop();

                delta += (double)m_endingAnimation.To - this.VerticalOffset;

                m_animation.From = this.VerticalOffset;
                m_animation.To = this.VerticalOffset + delta * 0.8;

                m_endingAnimation.From = m_animation.To;
                m_endingAnimation.To = this.VerticalOffset + delta;
            }
            else
            {

                m_storyboard.Stop();

                m_animation.From = this.VerticalOffset;
                m_animation.To = this.VerticalOffset + delta * 0.8;

                m_endingAnimation.From = m_animation.To;
                m_endingAnimation.To = this.VerticalOffset + delta;
            }

            m_storyboard.Begin();
            m_storyboardBegan = true;
        }
    }
}
