using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;

namespace SMART_NOC.Services
{
    /// <summary>
    /// ?? NEXT-GEN ANIMATION SYSTEM
    /// Advanced animations for modern WinUI3 applications
    /// </summary>
    public static class AnimationHelper
    {
        // ???????????????????????????????????????????????????????????
        // ?? PAGE TRANSITION ANIMATIONS
        // ???????????????????????????????????????????????????????????

        /// <summary>?? Fade In animation (smooth opacity transition)</summary>
        public static Storyboard CreateFadeInAnimation(UIElement element, double duration = 500, double delayMs = 0)
        {
            var storyboard = new Storyboard();
            if (delayMs > 0) storyboard.BeginTime = TimeSpan.FromMilliseconds(delayMs);
            
            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            
            Storyboard.SetTarget(fadeAnimation, element);
            Storyboard.SetTargetProperty(fadeAnimation, "(UIElement.Opacity)");
            storyboard.Children.Add(fadeAnimation);
            return storyboard;
        }

        /// <summary>?? Fade Out animation</summary>
        public static Storyboard CreateFadeOutAnimation(UIElement element, double duration = 500)
        {
            var storyboard = new Storyboard();
            
            var fadeAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            
            Storyboard.SetTarget(fadeAnimation, element);
            Storyboard.SetTargetProperty(fadeAnimation, "(UIElement.Opacity)");
            storyboard.Children.Add(fadeAnimation);
            return storyboard;
        }

        /// <summary>?? Slide In animation (from direction: Left, Right, Up, Down)</summary>
        public static Storyboard CreateSlideInAnimation(UIElement element, string direction = "Left", double duration = 500, double distance = 50, double delayMs = 0)
        {
            var storyboard = new Storyboard();
            if (delayMs > 0) storyboard.BeginTime = TimeSpan.FromMilliseconds(delayMs);
            
            if (element.RenderTransform == null)
                element.RenderTransform = new Microsoft.UI.Xaml.Media.CompositeTransform();
            
            var slideAnimation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            
            string property = direction switch
            {
                "Left" => "(UIElement.RenderTransform).(Microsoft.UI.Xaml.Media.CompositeTransform.TranslateX)",
                "Right" => "(UIElement.RenderTransform).(Microsoft.UI.Xaml.Media.CompositeTransform.TranslateX)",
                "Up" => "(UIElement.RenderTransform).(Microsoft.UI.Xaml.Media.CompositeTransform.TranslateY)",
                "Down" => "(UIElement.RenderTransform).(Microsoft.UI.Xaml.Media.CompositeTransform.TranslateY)",
                _ => "(UIElement.RenderTransform).(Microsoft.UI.Xaml.Media.CompositeTransform.TranslateX)"
            };
            
            slideAnimation.From = direction switch
            {
                "Left" => -distance,
                "Right" => distance,
                "Up" => -distance,
                "Down" => distance,
                _ => -distance
            };
            slideAnimation.To = 0;
            
            Storyboard.SetTarget(slideAnimation, element);
            Storyboard.SetTargetProperty(slideAnimation, property);
            storyboard.Children.Add(slideAnimation);
            return storyboard;
        }

        /// <summary>?? Scale (Zoom) In animation</summary>
        public static Storyboard CreateScaleInAnimation(UIElement element, double duration = 500, double scale = 1.1)
        {
            var storyboard = new Storyboard();
            
            if (element.RenderTransform == null)
                element.RenderTransform = new Microsoft.UI.Xaml.Media.CompositeTransform();
            
            var scaleXAnimation = new DoubleAnimation
            {
                From = 0.8,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
                EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1 }
            };
            
            var scaleYAnimation = new DoubleAnimation
            {
                From = 0.8,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
                EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1 }
            };
            
            Storyboard.SetTarget(scaleXAnimation, element);
            Storyboard.SetTargetProperty(scaleXAnimation, "(UIElement.RenderTransform).(Microsoft.UI.Xaml.Media.CompositeTransform.ScaleX)");
            
            Storyboard.SetTarget(scaleYAnimation, element);
            Storyboard.SetTargetProperty(scaleYAnimation, "(UIElement.RenderTransform).(Microsoft.UI.Xaml.Media.CompositeTransform.ScaleY)");
            
            storyboard.Children.Add(scaleXAnimation);
            storyboard.Children.Add(scaleYAnimation);
            return storyboard;
        }

        /// <summary>?? Rotation animation (spins element)</summary>
        public static Storyboard CreateRotationAnimation(UIElement element, double duration = 1000, int rotations = 1)
        {
            var storyboard = new Storyboard();
            
            if (element.RenderTransform == null)
            {
                var transform = new Microsoft.UI.Xaml.Media.CompositeTransform { CenterX = 0.5, CenterY = 0.5 };
                element.RenderTransform = transform;
            }
            
            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360 * rotations,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration))
            };
            
            Storyboard.SetTarget(rotateAnimation, element);
            Storyboard.SetTargetProperty(rotateAnimation, "(UIElement.RenderTransform).(Microsoft.UI.Xaml.Media.CompositeTransform.Rotation)");
            storyboard.Children.Add(rotateAnimation);
            return storyboard;
        }

        // ???????????????????????????????????????????????????????????
        // ?? INTERACTIVE ANIMATIONS
        // ???????????????????????????????????????????????????????????

        /// <summary>?? Button Hover animation (scale + glow effect)</summary>
        public static void ApplyButtonHoverAnimation(UIElement button)
        {
            button.PointerEntered += (s, e) =>
            {
                if (button.RenderTransform == null)
                    button.RenderTransform = new Microsoft.UI.Xaml.Media.CompositeTransform();
                    
                var animation = CreateScaleInAnimation(button, 200, 1.05);
                animation.Begin();
            };
            
            button.PointerExited += (s, e) =>
            {
                if (button.RenderTransform == null)
                    button.RenderTransform = new Microsoft.UI.Xaml.Media.CompositeTransform();
                
                var resetAnimation = new Storyboard();
                
                var scaleXReset = new DoubleAnimation
                {
                    To = 1,
                    Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                
                var scaleYReset = new DoubleAnimation
                {
                    To = 1,
                    Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
                };
                
                Storyboard.SetTarget(scaleXReset, button);
                Storyboard.SetTargetProperty(scaleXReset, "(UIElement.RenderTransform).(Microsoft.UI.Xaml.Media.CompositeTransform.ScaleX)");
                
                Storyboard.SetTarget(scaleYReset, button);
                Storyboard.SetTargetProperty(scaleYReset, "(UIElement.RenderTransform).(Microsoft.UI.Xaml.Media.CompositeTransform.ScaleY)");
                
                resetAnimation.Children.Add(scaleXReset);
                resetAnimation.Children.Add(scaleYReset);
                resetAnimation.Begin();
            };
        }

        /// <summary>?? Pulse animation (breathing effect)</summary>
        public static Storyboard CreatePulseAnimation(UIElement element, double duration = 2000)
        {
            var storyboard = new Storyboard();
            storyboard.RepeatBehavior = RepeatBehavior.Forever;
            
            var opacityAnimation = new DoubleAnimationUsingKeyFrames();
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 0.5 });
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(duration / 2), Value = 1.0 });
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(duration), Value = 0.5 });
            
            Storyboard.SetTarget(opacityAnimation, element);
            Storyboard.SetTargetProperty(opacityAnimation, "(UIElement.Opacity)");
            storyboard.Children.Add(opacityAnimation);
            return storyboard;
        }

        /// <summary>?? Loading spinner animation</summary>
        public static Storyboard CreateLoadingSpinnerAnimation(UIElement element, double duration = 2000)
        {
            return CreateRotationAnimation(element, duration, rotations: 1);
        }

        /// <summary>?? Bounce animation (elasticity effect)</summary>
        public static Storyboard CreateBounceAnimation(UIElement element, double distance = 20, double duration = 600)
        {
            var storyboard = new Storyboard();
            
            if (element.RenderTransform == null)
                element.RenderTransform = new Microsoft.UI.Xaml.Media.CompositeTransform();
            
            var bounceAnimation = new DoubleAnimation
            {
                From = 0,
                To = distance,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
                EasingFunction = new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 3 }
            };
            
            Storyboard.SetTarget(bounceAnimation, element);
            Storyboard.SetTargetProperty(bounceAnimation, "(UIElement.RenderTransform).(Microsoft.UI.Xaml.Media.CompositeTransform.TranslateY)");
            storyboard.Children.Add(bounceAnimation);
            return storyboard;
        }

        /// <summary>?? Shake animation (error feedback)</summary>
        public static Storyboard CreateShakeAnimation(UIElement element, double distance = 5, int shakes = 3)
        {
            var storyboard = new Storyboard();
            var totalDuration = shakes * 100;
            
            if (element.RenderTransform == null)
                element.RenderTransform = new Microsoft.UI.Xaml.Media.CompositeTransform();
            
            var shakeAnimation = new DoubleAnimationUsingKeyFrames();
            for (int i = 0; i <= shakes * 2; i++)
            {
                var offset = (i % 2 == 0) ? distance : -distance;
                shakeAnimation.KeyFrames.Add(
                    new LinearDoubleKeyFrame 
                    { 
                        KeyTime = TimeSpan.FromMilliseconds(i * (totalDuration / (shakes * 2))),
                        Value = offset 
                    }
                );
            }
            shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(totalDuration), Value = 0 });
            
            Storyboard.SetTarget(shakeAnimation, element);
            Storyboard.SetTargetProperty(shakeAnimation, "(UIElement.RenderTransform).(Microsoft.UI.Xaml.Media.CompositeTransform.TranslateX)");
            storyboard.Children.Add(shakeAnimation);
            return storyboard;
        }

        // ???????????????????????????????????????????????????????????
        // ?? LIST ANIMATIONS
        // ???????????????????????????????????????????????????????????

        /// <summary>?? Staggered list item animation</summary>
        public static async Task AnimateListItemsAsync(List<UIElement> items, double itemDelay = 100, double duration = 400)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var animation = CreateSlideInAnimation(items[i], "Left", duration, 30, itemDelay * i);
                animation.Begin();
                
                if (i < items.Count - 1)
                    await Task.Delay((int)itemDelay);
            }
        }

        /// <summary>?? Fade in list items with stagger</summary>
        public static async Task FadeInListItemsAsync(List<UIElement> items, double itemDelay = 100, double duration = 400)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var animation = CreateFadeInAnimation(items[i], duration, itemDelay * i);
                animation.Begin();
                
                if (i < items.Count - 1)
                    await Task.Delay((int)itemDelay);
            }
        }

        // ???????????????????????????????????????????????????????????
        // ?? ADVANCED ANIMATIONS
        // ???????????????????????????????????????????????????????????

        /// <summary>?? Flip animation (3D effect)</summary>
        public static Storyboard CreateFlipAnimation(UIElement element, double duration = 600)
        {
            var storyboard = new Storyboard();
            
            if (element.RenderTransform == null)
                element.RenderTransform = new Microsoft.UI.Xaml.Media.CompositeTransform();
            
            var flipAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromMilliseconds(duration)),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            
            Storyboard.SetTarget(flipAnimation, element);
            Storyboard.SetTargetProperty(flipAnimation, "(UIElement.RenderTransform).(Microsoft.UI.Xaml.Media.CompositeTransform.Rotation)");
            storyboard.Children.Add(flipAnimation);
            return storyboard;
        }

        /// <summary>?? Glow animation (expand and fade)</summary>
        public static Storyboard CreateGlowAnimation(UIElement element, double duration = 1000)
        {
            var storyboard = new Storyboard();
            storyboard.RepeatBehavior = RepeatBehavior.Forever;
            
            if (element.RenderTransform == null)
                element.RenderTransform = new Microsoft.UI.Xaml.Media.CompositeTransform();
            
            var scaleAnimation = new DoubleAnimationUsingKeyFrames();
            scaleAnimation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 1.0 });
            scaleAnimation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(duration / 2), Value = 1.2 });
            scaleAnimation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(duration), Value = 1.0 });
            
            var opacityAnimation = new DoubleAnimationUsingKeyFrames();
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = 1.0 });
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(duration / 2), Value = 0.5 });
            opacityAnimation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = TimeSpan.FromMilliseconds(duration), Value = 1.0 });
            
            Storyboard.SetTarget(scaleAnimation, element);
            Storyboard.SetTargetProperty(scaleAnimation, "(UIElement.RenderTransform).(Microsoft.UI.Xaml.Media.CompositeTransform.ScaleX)");
            
            Storyboard.SetTarget(opacityAnimation, element);
            Storyboard.SetTargetProperty(opacityAnimation, "(UIElement.Opacity)");
            
            storyboard.Children.Add(scaleAnimation);
            storyboard.Children.Add(opacityAnimation);
            return storyboard;
        }
    }
}
