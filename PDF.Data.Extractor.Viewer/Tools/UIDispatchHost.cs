// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.Tools
{
    using System;
    using System.Windows.Threading;

    public static class UIDispatchHost
    {
        private static Dispatcher s_dispatcher;

        public static void RegisterDispatcher(Dispatcher dispatcher)
        {
            s_dispatcher = dispatcher;
        }

        public static void Call(Action action)
        {
            s_dispatcher!.BeginInvoke(action);
        }
    }
}
