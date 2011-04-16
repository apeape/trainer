using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;

namespace trainer
{
    public static class ImageBehavior
    {
        public static void DoWhenLoaded<T>(this T element, Action<T> action)
            where T : FrameworkElement
        {
            if (element.IsLoaded)
            {
                action(element);
            }
            else
            {
                RoutedEventHandler handler = null;
                handler = (sender, e) =>
                {
                    element.Loaded -= handler;
                    action(element);
                };
                element.Loaded += handler;
            }
        }

        #region AnimatedSource

        [AttachedPropertyBrowsableForType(typeof(Image))]
        public static ImageSource GetAnimatedSource(Image obj)
        {
            return (ImageSource)obj.GetValue(AnimatedSourceProperty);
        }

        public static void SetAnimatedSource(Image obj, ImageSource value)
        {
            obj.SetValue(AnimatedSourceProperty, value);
        }

        public static readonly DependencyProperty AnimatedSourceProperty =
            DependencyProperty.RegisterAttached(
              "AnimatedSource",
              typeof(ImageSource),
              typeof(ImageBehavior),
              new UIPropertyMetadata(
                null,
                AnimatedSourceChanged));

        private static void AnimatedSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            Image imageControl = o as Image;
            if (imageControl == null)
                return;

            var oldValue = e.OldValue as ImageSource;
            var newValue = e.NewValue as ImageSource;
            if (oldValue != null)
            {
                imageControl.BeginAnimation(Image.SourceProperty, null);
            }
            if (newValue != null)
            {
                imageControl.DoWhenLoaded(InitAnimationOrImage);
            }
        }

        private static void InitAnimationOrImage(Image imageControl)
        {
            BitmapSource source = GetAnimatedSource(imageControl) as BitmapSource;
            if (source != null)
            {
                var decoder = GetDecoder(source) as GifBitmapDecoder;
                if (decoder != null && decoder.Frames.Count > 1)
                {
                    var animation = new ObjectAnimationUsingKeyFrames();
                    var keyTime = TimeSpan.Zero;
                    var totalDuration = TimeSpan.Zero;
                    foreach (var frame in decoder.Frames)
                    {
                        var keyFrame = new DiscreteObjectKeyFrame(frame, keyTime);
                        animation.KeyFrames.Add(keyFrame);
                        var duration = GetFrameDelay(frame);
                        totalDuration += duration;
                        keyTime = keyTime + duration;
                    }
                    animation.Duration = totalDuration;
                    animation.RepeatBehavior = RepeatBehavior.Forever;
                    imageControl.Source = decoder.Frames[0];
                    imageControl.BeginAnimation(Image.SourceProperty, animation);
                    return;
                }
            }
            imageControl.Source = source;
            return;
        }

        private static BitmapDecoder GetDecoder(BitmapSource image)
        {
            BitmapDecoder decoder = null;
            var frame = image as BitmapFrame;
            if (frame != null)
                decoder = frame.Decoder;

            if (decoder == null)
            {
                var bmp = image as BitmapImage;
                if (bmp != null)
                {
                    if (bmp.StreamSource != null)
                    {
                        decoder = BitmapDecoder.Create(bmp.StreamSource, bmp.CreateOptions, bmp.CacheOption);
                    }
                    else if (bmp.UriSource != null)
                    {
                        decoder = BitmapDecoder.Create(bmp.UriSource, bmp.CreateOptions, bmp.CacheOption);
                    }
                }
            }

            return decoder;
        }

        private static TimeSpan GetFrameDelay(BitmapFrame frame)
        {
            // 100ms by default if the value is not defined in metadata
            TimeSpan duration = TimeSpan.FromMilliseconds(100);

            BitmapMetadata metaData = null;
            try
            {
                metaData = frame.Metadata as BitmapMetadata;
                if (metaData != null)
                {
                    const string query = "/grctlext/Delay";
                    if (metaData.ContainsQuery(query))
                    {
                        object o = metaData.GetQuery(query);
                        if (o != null)
                        {
                            ushort delay = (ushort)o;
                            duration = TimeSpan.FromMilliseconds(10 * delay);
                        }
                    }
                }
            }
            catch (NotSupportedException)
            {
            }

            return duration;
        }

        #endregion
    }
}
