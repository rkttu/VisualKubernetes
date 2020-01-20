using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandReplicaSetResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1ReplicaSetList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker replicaSetsLoader;

        private void InitializeReplicaSetLoader()
        {
            replicaSetsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            replicaSetsLoader.DoWork += ReplicaSetsLoader_DoWork;
            replicaSetsLoader.RunWorkerCompleted += ReplicaSetsLoader_RunWorkerCompleted;
            container.Add(replicaSetsLoader);
        }

        private void ReplicaSetsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandReplicaSetResponse(
                args.Item1,
                client.ListNamespacedReplicaSet(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void ReplicaSetsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load replica sets due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load replica sets due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandReplicaSetResponse;
            var replicaSetList = response.Item2;
            if (replicaSetList == null)
            {
                MessageBox.Show(this,
                    "Cannot load replica sets due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var replicaSetsTreeNode = response.Item3;
            replicaSetsTreeNode.Nodes.Clear();
            var replicaSetNodes = new List<TreeNode>();
            for (int i = 0, max = replicaSetList.Items.Count; i < max; i++)
            {
                var eachReplicaSetTreeNode = new TreeNode(replicaSetList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, replicaSetList.Items[i]),
                    ImageKey = "rs",
                    SelectedImageKey = "rs",
                };
                replicaSetNodes.Add(eachReplicaSetTreeNode);
            }
            replicaSetsTreeNode.Nodes.AddRange(replicaSetNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1ReplicaSet o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedReplicaSetAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Replica Set - {fetched.Metadata.Name}";
        }
    }
}
