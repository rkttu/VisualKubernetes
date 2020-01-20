using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandStatefulSetResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1StatefulSetList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker statefulSetsLoader;

        private void InitializeStatefulSetLoader()
        {
            statefulSetsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            statefulSetsLoader.DoWork += StatefulSetsLoader_DoWork;
            statefulSetsLoader.RunWorkerCompleted += StatefulSetsLoader_RunWorkerCompleted;
            container.Add(statefulSetsLoader);
        }

        private void StatefulSetsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandStatefulSetResponse(
                args.Item1,
                client.ListNamespacedStatefulSet(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void StatefulSetsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load stateful sets due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load stateful sets due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandStatefulSetResponse;
            var statefulSetList = response.Item2;
            if (statefulSetList == null)
            {
                MessageBox.Show(this,
                    "Cannot load stateful sets due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var statefulSetsTreeNode = response.Item3;
            statefulSetsTreeNode.Nodes.Clear();
            var statefulSetNodes = new List<TreeNode>();
            for (int i = 0, max = statefulSetList.Items.Count; i < max; i++)
            {
                var eachStatefulSetTreeNode = new TreeNode(statefulSetList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, statefulSetList.Items[i]),
                    ImageKey = "sts",
                    SelectedImageKey = "sts",
                };
                statefulSetNodes.Add(eachStatefulSetTreeNode);
            }
            statefulSetsTreeNode.Nodes.AddRange(statefulSetNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1StatefulSet o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedStatefulSetAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Stateful Set - {fetched.Metadata.Name}";
        }
    }
}
