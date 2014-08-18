using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;

namespace zebra
{
    public class WindowAddtionInfo
    {
        public bool CloseBecauseNavOut = false;
        public bool AcceptClose = false;
        public bool InClosingCallStack = false;

        static private WindowAddtionInfo retriveOrCreateInfo(Window window)
        {
            if (window.Tag != null && window.Tag as WindowAddtionInfo == null)
            {
                throw new InvalidOperationException();
            }

            WindowAddtionInfo info = window.Tag as WindowAddtionInfo;

            if (info == null)
            {
                window.Tag = info = new WindowAddtionInfo();
            }

            return info;
        }

        static public void SetCloseBecauseNavOut(Window window, bool v)
        {
            retriveOrCreateInfo(window).CloseBecauseNavOut = v;
        }

        static public bool GetCloseBecauseNavOut(Window window)
        {
            WindowAddtionInfo info = null;

            info = window.Tag as WindowAddtionInfo;

            return info != null ? info.CloseBecauseNavOut : false;
        }

        static public void SetAcceptClose(Window window, bool v)
        {
            retriveOrCreateInfo(window).AcceptClose = v;
        }

        static public bool GetAcceptClose(Window window)
        {
            WindowAddtionInfo info = null;

            info = window.Tag as WindowAddtionInfo;

            return info != null ? info.AcceptClose : false;
        }

        static public void SetInClosingCallStack(Window window, bool v)
        {
            retriveOrCreateInfo(window).InClosingCallStack = v;
        }

        static public bool GetInClosingCallStack(Window window)
        {
            WindowAddtionInfo info = null;

            info = window.Tag as WindowAddtionInfo;

            return info != null ? info.InClosingCallStack : false;
        }
    }

    public interface IView
    {
        void NavIn(bool dialog = false, Window owner = null);
        void NavOut();
    }

    public class ViewBase<T> : IView where T : Window
    {
        protected T window;
        public event EventHandler WannaClose;
        public T Window
        {
            get
            {
                return window;
            }
        }

        public void NavIn(bool dialog = false, Window owner = null)
        {
            // 如果窗体已经存在，尚未关闭，则忽略本次调用

            if (window != null) return;

            // 子类将重写 beforeNavIn() 以完成窗体的初始化等工作

            beforeNavIn();
            addCloseLogic();

            // 好了，显示窗体
            // 支持模态和非模态方式

            window.Owner = owner;

            if (window != null)
            {
                if (dialog)
                {
                    window.ShowDialog();
                }
                else
                {
                    window.Show();
                }
            }
        }

        public void NavOut()
        {
            // 如果根本就没有任何窗体，则忽略本次调用

            if (window == null) return;

            // 子类将重写 beforeNavOut() 以完成窗体的初始化等工作

            beforeNavOut();

            // 执行关闭窗口的操作

            if (WindowAddtionInfo.GetInClosingCallStack(window))
            {
                // 当前调用是位于 Closing 调用栈里
                // 我们只需把相关字段设为 true 就可以完成关闭
                // 不能去调用 Close() 方法，否则会抛出异常

                WindowAddtionInfo.SetAcceptClose(window, true);
            }
            else
            {
                // 直接调用 Close() 就可以关闭窗口了
                // 为了避免事件重复触发，这里需要在 Close() 前做个标记

                WindowAddtionInfo.SetCloseBecauseNavOut(window, true);
                window.Close();
            }
        }

        protected virtual void beforeNavIn()
        {
            // 由子类负责完成
        }

        protected virtual void beforeNavOut()
        {
            // 由子类负责完成
        }

        protected void addCloseLogic()
        {
            if (window == null) return;
            window.Closing += window_Closing;
            window.Closed += window_Closed;
        }

        protected void removeCloseLogic()
        {
            if (window == null) return;
            window.Closing -= window_Closing;
            window.Closed -= window_Closed;
        }

        void window_Closing(object sender, CancelEventArgs e)
        {
            // 如果是 NavOut 导致的关闭，则不作任何处理，直接关闭，不会触发 WannaClose 事件
            // 否则认为是用户点击右上角的关闭按钮，发出 WannaClose 通知交由外部处理

            bool userClickCloseButton = !WindowAddtionInfo.GetCloseBecauseNavOut(window);

            if (userClickCloseButton)
            {
                if (WannaClose != null)
                {
                    WindowAddtionInfo.SetAcceptClose(window, false);
                    WindowAddtionInfo.SetInClosingCallStack(window, true);
                    try
                    {
                        WannaClose(this, EventArgs.Empty);
                    }
                    finally
                    {
                        WindowAddtionInfo.SetInClosingCallStack(window, false);
                    }
                }

                e.Cancel = !WindowAddtionInfo.GetAcceptClose(window);
            }
        }

        void window_Closed(object sender, EventArgs e)
        {
            removeCloseLogic();
            window = null;
        }

    }

    public class LoginView : ViewBase<LoginWindow>
    {
        public event EventHandler WannaLogin;
        
        public string Username
        {
            get
            {
                if (window == null)
                {
                    return string.Empty;
                }
                else
                {
                    return window.usernameTextBox.Text;
                }
            }
            set
            {
                if (window == null)
                {
                    // do nothing
                }
                else
                {
                    window.usernameTextBox.Text = value;
                }
            }
        }

        public string Password
        {
            get
            {
                if (window == null)
                {
                    return string.Empty;
                }
                else
                {
                    return window.passwordBox.Password;
                }
            }
            set
            {
                if (window == null)
                {
                    // do nothing
                }
                else
                {
                    window.passwordBox.Password = value;
                }
            }
        }

        protected override void beforeNavIn()
        {
            window = new LoginWindow();
            window.loginButton.Click += loginButton_Click;
        }

        protected override void beforeNavOut()
        {
            window.loginButton.Click -= loginButton_Click;
        }

        void loginButton_Click(object sender, RoutedEventArgs e)
        {
            if (WannaLogin != null)
            {
                WannaLogin(this, EventArgs.Empty);
            }
        }

    }

    public class SelectServerView : ViewBase<SelectServerWindow>
    {
        public event EventHandler WannaStart;
        public event EventHandler WannaConfig;

        protected override void beforeNavIn()
        {
            window = new SelectServerWindow();
            window.startButton.Click += startButton_Click;
            window.configButton.Click += configButton_Click;
        }

        protected override void beforeNavOut()
        {
            window.startButton.Click -= startButton_Click;
            window.configButton.Click -= configButton_Click;
        }

        void configButton_Click(object sender, RoutedEventArgs e)
        {
            if (WannaConfig != null)
            {
                WannaConfig(this, EventArgs.Empty);
            }
        }

        void startButton_Click(object sender, RoutedEventArgs e)
        {
            if (WannaStart != null)
            {
                WannaStart(this, EventArgs.Empty);
            }
        }
    }

    public class ConfigView : ViewBase<ConfigWindow>
    {
        protected override void beforeNavIn()
        {
            window = new ConfigWindow();
        }
    }

    public class WorkDetailView : ViewBase<WorkDetailWindow>
    {
        public event EventHandler WannaStop;

        protected override void beforeNavIn()
        {
            window = new WorkDetailWindow();
            window.stopButton.Click += stopButton_Click;
        }

        protected override void beforeNavOut()
        {
            window.stopButton.Click -= stopButton_Click;
        }

        void stopButton_Click(object sender, RoutedEventArgs e)
        {
            if (WannaStop != null)
            {
                WannaStop(this, EventArgs.Empty);
            }
        }
    }

    public class ViewManager
    {
        LoginView loginView = new LoginView();
        SelectServerView selectServerView = new SelectServerView();
        ConfigView configView = new ConfigView();
        WorkDetailView workDetailView = new WorkDetailView();

        public ViewManager()
        {
            loginView.WannaLogin += loginView_WannaLogin;
            loginView.WannaClose += loginView_WannaClose;

            selectServerView.WannaStart += selectServerView_WannaStart;
            selectServerView.WannaConfig += selectServerView_WannaConfig;
            selectServerView.WannaClose += selectServerView_WannaClose;

            configView.WannaClose += configView_WannaClose;

            workDetailView.WannaStop += workDetailView_WannaStop;
            workDetailView.WannaClose += workDetailView_WannaClose;

            loginView.NavIn();
        }

        void loginView_WannaClose(object sender, EventArgs e)
        {
            shutdown();
        }

        void selectServerView_WannaClose(object sender, EventArgs e)
        {
            selectServerView.NavOut();
            loginView.NavIn();
        }

        void workDetailView_WannaStop(object sender, EventArgs e)
        {
            workDetailView.NavOut();
            selectServerView.NavIn();
        }

        void workDetailView_WannaClose(object sender, EventArgs e)
        {
            workDetailView.NavOut();
            selectServerView.NavIn();
        }

        void configView_WannaClose(object sender, EventArgs e)
        {
            configView.NavOut();
        }

        void selectServerView_WannaConfig(object sender, EventArgs e)
        {
            configView.NavIn(true, selectServerView.Window);
        }

        void selectServerView_WannaStart(object sender, EventArgs e)
        {
            selectServerView.NavOut();
            workDetailView.NavIn();
        }

        void loginView_WannaLogin(object sender, EventArgs e)
        {
            loginView.NavOut();
            selectServerView.NavIn();
        }


        void shutdown()
        {
            Application.Current.Shutdown(0);
        }


    }
}
