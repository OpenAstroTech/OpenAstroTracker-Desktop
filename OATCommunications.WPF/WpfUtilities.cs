using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Threading;

namespace OATCommunications.WPF
{
    public class WpfUtilities
    {
        /// <summary>
        /// Runs the given action using the given dispatcher to determine the thread to run on. 
        /// </summary>
        /// <param name="action">The action.</param>
        public static void RunOnUiThread(Action action, Dispatcher dispatcher)
        {
            // If we're on another thread, check whether the main thread dispatcher is available
            // If the dispatcher is available, do a synchronous update, else asynchronous
            if (dispatcher.CheckAccess())
            {
                // If the dispatcher is available, do a synchronous update
                action();
            }
            else
            {
                try
                {
                    // If the dispatcher is available, do an asynchronous update
                    dispatcher.Invoke(action);
                }
                catch (Exception e)
                {
                    dispatcher.BeginInvoke(action);
                }
            }
        }
    }

    public class ScrollIntoViewForListBox : Behavior<ListBox>
    {
        /// <summary>
        ///  When Beahvior is attached
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.SelectionChanged += AssociatedObject_SelectionChanged;
        }

        /// <summary>
        /// On Selection Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AssociatedObject_SelectionChanged(object sender,
                                               SelectionChangedEventArgs e)
        {
            if (sender is ListBox)
            {
                ListBox listBox = (sender as ListBox);
                if (listBox.SelectedItem != null)
                {
                    listBox.Dispatcher.BeginInvoke(
                        (Action)(() =>
                        {
                            listBox.UpdateLayout();
                            if (listBox.SelectedItem !=
                            null)
                                listBox.ScrollIntoView(
                                listBox.SelectedItem);
                        }));
                }
            }
        }
        /// <summary>
        /// When behavior is detached
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.SelectionChanged -=
                AssociatedObject_SelectionChanged;

        }
    }
}
