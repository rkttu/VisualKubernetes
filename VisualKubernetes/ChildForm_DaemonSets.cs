using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandDaemonSetResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1DaemonSetList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker daemonSetsLoader;

        private void InitializeDaemonSetLoader()
        {
            daemonSetsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            daemonSetsLoader.DoWork += DaemonSetsLoader_DoWork;
            daemonSetsLoader.RunWorkerCompleted += DaemonSetsLoader_RunWorkerCompleted;
            container.Add(daemonSetsLoader);
        }

        private void DaemonSetsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandDaemonSetResponse(
                args.Item1,
                client.ListNamespacedDaemonSet(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void DaemonSetsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load daemon sets due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load daemon sets due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandDaemonSetResponse;
            var daemonSetList = response.Item2;
            if (daemonSetList == null)
            {
                MessageBox.Show(this,
                    "Cannot load daemon sets due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var daemonSetsTreeNode = response.Item3;
            daemonSetsTreeNode.Nodes.Clear();
            var daemonSetNodes = new List<TreeNode>();
            for (int i = 0, max = daemonSetList.Items.Count; i < max; i++)
            {
                var eachDaemonSetTreeNode = new TreeNode(daemonSetList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, daemonSetList.Items[i]),
                    ImageKey = "ds",
                    SelectedImageKey = "ds",
                };
                daemonSetNodes.Add(eachDaemonSetTreeNode);
            }
            daemonSetsTreeNode.Nodes.AddRange(daemonSetNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1DaemonSet o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedDaemonSetAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Daemon Set - {fetched.Metadata.Name}";
        }
    }
}
