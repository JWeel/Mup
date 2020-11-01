using Mup.Extensions;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Mup.Controls
{
    public partial class ErrorFrame : UserControl
    {
        #region Constructors

        public ErrorFrame(Exception exception)
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.ErrorMessage = exception.JoinMessageWithInnerMessages();
            this.StackTrace = exception.StackTrace;
        }

        #endregion

        #region Properties

        public string ErrorMessage { get; }

        public string StackTrace { get; }

        public event Action<ErrorFrame> OnClose;

        public string ErrorDump => this.ErrorMessage + Environment.NewLine.Repeat(2) + this.StackTrace;

        #endregion

        #region Methods

        protected void Copy(object sender, RoutedEventArgs e) =>
            Clipboard.SetText(this.ErrorDump);

        protected void Close(object sender, RoutedEventArgs e) =>
            this.OnClose?.Invoke(this);

        #endregion
    }
}