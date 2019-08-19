// Copyright © 2011 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CefSharp.Example;
using CefSharp.Wpf.Example.Controls;
using CefSharp.Wpf.Example.ViewModels;
using Microsoft.Win32;

namespace CefSharp.Wpf.Example
{
    public partial class MainWindow : Window
    {
        private const string DefaultUrlForAddedTabs = "https://www.google.com";

        public ObservableCollection<BrowserTabViewModel> BrowserTabs { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            BrowserTabs = new ObservableCollection<BrowserTabViewModel>();

            CommandBindings.Add(new CommandBinding(ApplicationCommands.New, OpenNewTab));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, CloseTab));

            CommandBindings.Add(new CommandBinding(CefSharpCommands.Exit, Exit));
            CommandBindings.Add(new CommandBinding(CefSharpCommands.OpenTabCommand, OpenTabCommandBinding));
            CommandBindings.Add(new CommandBinding(CefSharpCommands.PrintTabToPdfCommand, PrintToPdfCommandBinding));
            CommandBindings.Add(new CommandBinding(CefSharpCommands.CustomCommand, CustomCommandBinding));

            Loaded += MainWindowLoaded;

            var bitness = Environment.Is64BitProcess ? "x64" : "x86";
            Title += " - " + bitness;
        }

        private void CloseTab(object sender, ExecutedRoutedEventArgs e)
        {
            if (BrowserTabs.Count > 0)
            {
                //Obtain the original source element for this event
                var originalSource = (FrameworkElement)e.OriginalSource;

                BrowserTabViewModel browserViewModel;

                if (originalSource is MainWindow)
                {
                    browserViewModel = BrowserTabs[TabControl.SelectedIndex];
                    BrowserTabs.RemoveAt(TabControl.SelectedIndex);
                }
                else
                {
                    //Remove the matching DataContext from the BrowserTabs collection
                    browserViewModel = (BrowserTabViewModel)originalSource.DataContext;
                    BrowserTabs.Remove(browserViewModel);
                }

                browserViewModel.WebBrowser.Dispose();
            }
        }

        private void CloseTab(BrowserTabViewModel browserViewModel)
        {
            if (BrowserTabs.Remove(browserViewModel))
            {
                browserViewModel.WebBrowser?.Dispose();
            }
        }

        private void OpenNewTab(object sender, ExecutedRoutedEventArgs e)
        {
            CreateNewTab();

            TabControl.SelectedIndex = TabControl.Items.Count - 1;
        }

        private void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            CreateNewTab(CefExample.DefaultUrl, true);

            // Reproduce the hack introduced here: https://github.com/cefsharp/CefSharp/commit/0d67b05513c2f716db8151d763eaf021de993e4f
            CreateNewTab();
            CreateNewTab();
        }

        private void CreateNewTab(string url = DefaultUrlForAddedTabs, bool showSideBar = false)
        {
            BrowserTabs.Add(new BrowserTabViewModel(url) { ShowSidebar = showSideBar });
        }

        private void CustomCommandBinding(object sender, ExecutedRoutedEventArgs e)
        {
            var param = e.Parameter.ToString();

            if (BrowserTabs.Count > 0)
            {
                var originalSource = (FrameworkElement)e.OriginalSource;

                //TODO: Remove duplicate code
                BrowserTabViewModel browserViewModel;

                if (originalSource is MainWindow)
                {
                    browserViewModel = BrowserTabs[TabControl.SelectedIndex];
                }
                else
                {
                    browserViewModel = (BrowserTabViewModel)originalSource.DataContext;
                }

                if (param == "CustomRequest")
                {
                    browserViewModel.LoadCustomRequestExample();
                }

                if (param == "OpenDevTools")
                {
                    browserViewModel.WebBrowser.ShowDevTools();
                }

                if (param == "ZoomIn")
                {
                    var cmd = browserViewModel.WebBrowser.ZoomInCommand;
                    cmd.Execute(null);
                }

                if (param == "ZoomOut")
                {
                    var cmd = browserViewModel.WebBrowser.ZoomOutCommand;
                    cmd.Execute(null);
                }

                if (param == "ZoomReset")
                {
                    var cmd = browserViewModel.WebBrowser.ZoomResetCommand;
                    cmd.Execute(null);
                }

                if (param == "ToggleSidebar")
                {
                    browserViewModel.ShowSidebar = !browserViewModel.ShowSidebar;
                }

                if (param == "ToggleDownloadInfo")
                {
                    //browserViewModel.ShowDownloadInfo = !browserViewModel.ShowDownloadInfo;

                    DispatcherTimer timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromMilliseconds(50);
                    timer.Tick += Timer_Tick;
                    timer.Start();
                }

                if (param == "AsyncJsbTaskTests")
                {
                    //After this setting has changed all tests will run through the Concurrent MethodQueueRunner
                    CefSharpSettings.ConcurrentTaskExecution = true;

                    CreateNewTab(CefExample.BindingTestsAsyncTaskUrl, true);

                    TabControl.SelectedIndex = TabControl.Items.Count - 1;
                }

                //NOTE: Add as required
                //else if (param == "CustomRequest123")
                //{
                //    browserViewModel.LoadCustomRequestExample();
                //}
            }
        }

        private int count;
        private Random random = new Random();

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (count++ < 100)
            {
                WindowState = WindowState.Normal;
                Width = Width > 500 ? Width - 5 : 1000;
                Height = Height > 500 ? Height - 5 : 1000;

                // Get all indexes but the selected one
                var indexes = new List<int>();
                for (int i = 0; i < TabControl.Items.Count; i++)
                {
                    if (TabControl.SelectedIndex != i)
                    {
                        indexes.Add(i);
                    }
                }

                // Select a random unselected tab
                TabControl.SelectedIndex = indexes[random.Next(0, indexes.Count)];

                // Close a tab and create a tab once in a while
                if (random.Next(0, 5) == 0)
                {
                    CloseTab(BrowserTabs[Math.Max(1, TabControl.SelectedIndex)]); // Don't close the first tab
                    CreateNewTab();
                }
            }
        }

        private async void PrintToPdfCommandBinding(object sender, ExecutedRoutedEventArgs e)
        {
            if (BrowserTabs.Count > 0)
            {
                var originalSource = (FrameworkElement)e.OriginalSource;

                BrowserTabViewModel browserViewModel;

                if (originalSource is MainWindow)
                {
                    browserViewModel = BrowserTabs[TabControl.SelectedIndex];
                }
                else
                {
                    browserViewModel = (BrowserTabViewModel)originalSource.DataContext;
                }

                var dialog = new SaveFileDialog
                {
                    DefaultExt = ".pdf",
                    Filter = "Pdf documents (.pdf)|*.pdf"
                };

                if (dialog.ShowDialog() == true)
                {
                    var success = await browserViewModel.WebBrowser.PrintToPdfAsync(dialog.FileName, new PdfPrintSettings
                    {
                        MarginType = CefPdfPrintMarginType.Custom,
                        MarginBottom = 10,
                        MarginTop = 0,
                        MarginLeft = 20,
                        MarginRight = 10,
                    });

                    if (success)
                    {
                        MessageBox.Show("Pdf was saved to " + dialog.FileName);
                    }
                    else
                    {
                        MessageBox.Show("Unable to save Pdf, check you have write permissions to " + dialog.FileName);
                    }

                }
            }
        }

        private void OpenTabCommandBinding(object sender, ExecutedRoutedEventArgs e)
        {
            var url = e.Parameter.ToString();

            if (string.IsNullOrEmpty(url))
            {
                throw new Exception("Please provide a valid command parameter for binding");
            }

            CreateNewTab(url, true);

            TabControl.SelectedIndex = TabControl.Items.Count - 1;
        }

        private void Exit(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }
    }
}
