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

namespace AllDataSheetFinder
{
    /// <summary>
    /// Interaction logic for ActionDialogWindow.xaml
    /// </summary>
    public partial class ActionDialogWindow : Window
    {
        public ActionDialogWindow(ActionDialogViewModel viewModel)
        {
            InitializeComponent();

            this.DataContext = viewModel;

            Global.Dialogs.Register(this, viewModel);
        }
    }
}
