﻿/*
 * PhoneGap is available under *either* the terms of the modified BSD license *or* the
 * MIT License (2008). See http://opensource.org/licenses/alphabetical for full text.
 *
 * Copyright (c) 2005-2011, Nitobi Software Inc.
 * Copyright (c) 2011, Microsoft Corporation
 */

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace WP7GapClassLib.PhoneGap.Commands
{

    public class DebugConsole : BaseCommand
    {
        // warn, error
        public void log(string msg)
        {
            Debug.WriteLine("Log:" + msg);
        }

        public void error(string msg)
        {
            Debug.WriteLine("Error:" + msg);
        }

        public void warn(string msg)
        {
            Debug.WriteLine("Warn:" + msg);
        }

    }
}
