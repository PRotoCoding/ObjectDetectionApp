using ObjectDetectionApp.Logic;
using ObjectDetectionLogic;
using Plugin.DeviceOrientation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace ObjectDetectionApp.Presentation
{
	public class PreviewPage : ContentPage
	{
        // This dictionary will be used by custom renderers for prediction drawing
        public Dictionary<string, Color> ClassColors;

        public DarknetService UnderlyingService { get; private set; }
        public PreviewPage(DarknetService darknetService) {

            // We do not want to use the smartphone horizontal
            CrossDeviceOrientation.Current.LockOrientation(Plugin.DeviceOrientation.Abstractions.DeviceOrientations.Landscape);
            // Remove NavigationBar
            NavigationPage.SetHasNavigationBar(this, false);
            ClassColors = new Dictionary<string, Color>();
            UnderlyingService = darknetService;
            
            UnderlyingService.ClassCollectionAquired += (o, e) =>
            {
                // Whenever a new class collection is received, new colors will be calculated
                // This algorithm ensures that only high contrast colors are used for prediction drawing
                ClassColors.Clear();
                int step_number = (int)Math.Ceiling((float)e.classes.Count / 6);
                double step_width = 1.0 / (step_number - 1);
                int count = 0;

                for (int i = 0; i < step_number; i++)
                {
                    AddDictonaryEntry(e, 0, 1, (float)(i * step_width), count++);
                    AddDictonaryEntry(e, (float)(i * step_width), 0, 1, count++);
                    AddDictonaryEntry(e, (float)(i * step_width), 1, 0, count++);
                    AddDictonaryEntry(e, 1, 0, (float)(i * step_width), count++);
                    AddDictonaryEntry(e, 0, (float)(i * step_width), 1, count++);
                    AddDictonaryEntry(e, 1, (float)(i * step_width), 0, count++);

                }
            };

            Disappearing += (o, e) =>
            {
                darknetService.StopService();
                CrossDeviceOrientation.Current.UnlockOrientation();
            };
        }
        private void AddDictonaryEntry(ClassCollection classCollection, float r, float g, float b, int count)
        {
            if (count < classCollection.classes.Count)
                ClassColors.Add(classCollection.classes[count], new Color(r, g, b));
        }
    }
}