using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandEventResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1EventList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker eventsLoader;

        private void InitializeEventLoader()
        {
            eventsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            eventsLoader.DoWork += EventsLoader_DoWork;
            eventsLoader.RunWorkerCompleted += EventsLoader_RunWorkerCompleted;
            container.Add(eventsLoader);
        }

        private void EventsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandEventResponse(
                args.Item1,
                client.ListNamespacedEvent(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void EventsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load events due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load events due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandEventResponse;
            var eventList = response.Item2;
            if (eventList == null)
            {
                MessageBox.Show(this,
                    "Cannot load events due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var eventsTreeNode = response.Item3;
            eventsTreeNode.Nodes.Clear();
            var eventNodes = new List<TreeNode>();
            for (int i = 0, max = eventList.Items.Count; i < max; i++)
            {
                var eachEventTreeNode = new TreeNode(eventList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, eventList.Items[i]),
                    ImageKey = "event",
                    SelectedImageKey = "event",
                };
                eventNodes.Add(eachEventTreeNode);
            }
            eventsTreeNode.Nodes.AddRange(eventNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1Event o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedEventAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Event - {fetched.Metadata.Name}";
        }
    }
}
