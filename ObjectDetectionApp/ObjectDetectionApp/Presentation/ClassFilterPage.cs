using ObjectDetectionApp.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace ObjectDetectionApp.Presentation
{
	public class ClassFilterPage : ContentPage
	{
		public ClassFilterPage(DarknetService darknetService)
		{
            var section = new TableSection("Classes");
            NavigationPage.SetHasNavigationBar(this, false);
            foreach(var obj in darknetService.ClassCollection.classes)
            {
                var switchCell = new SwitchCell() { Text = obj };
                switchCell.On = darknetService.FilteredClasses.Contains(obj) ? true : false;
                switchCell.OnChanged += (o, e) =>
                {
                    if(e.Value)
                        darknetService.FilteredClasses.Add((o as SwitchCell).Text);
                    else
                        darknetService.FilteredClasses.Remove((o as SwitchCell).Text);
                };
                section.Add(switchCell);
            }
            Content = new TableView() { Root = new TableRoot{ section } };
		}
	}
}