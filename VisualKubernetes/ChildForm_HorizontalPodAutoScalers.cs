using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandHorizontalPodAutoscalerResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1HorizontalPodAutoscalerList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker horizontalPodAutoscalersLoader;

        private void InitializeHorizontalPodAutoscalerLoader()
        {
            horizontalPodAutoscalersLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            horizontalPodAutoscalersLoader.DoWork += HorizontalPodAutoscalersLoader_DoWork;
            horizontalPodAutoscalersLoader.RunWorkerCompleted += HorizontalPodAutoscalersLoader_RunWorkerCompleted;
            container.Add(horizontalPodAutoscalersLoader);
        }

        private void HorizontalPodAutoscalersLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandHorizontalPodAutoscalerResponse(
                args.Item1,
                client.ListNamespacedHorizontalPodAutoscaler(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void HorizontalPodAutoscalersLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load horizontal pod autoscalers due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load horizontal pod autoscalers due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandHorizontalPodAutoscalerResponse;
            var horizontalPodAutoscalerList = response.Item2;
            if (horizontalPodAutoscalerList == null)
            {
                MessageBox.Show(this,
                    "Cannot load horizontal pod autoscalers due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var horizontalPodAutoscalersTreeNode = response.Item3;
            horizontalPodAutoscalersTreeNode.Nodes.Clear();
            var horizontalPodAutoscalerNodes = new List<TreeNode>();
            for (int i = 0, max = horizontalPodAutoscalerList.Items.Count; i < max; i++)
            {
                var eachHorizontalPodAutoscalerTreeNode = new TreeNode(horizontalPodAutoscalerList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, horizontalPodAutoscalerList.Items[i]),
                    ImageKey = "hpa",
                    SelectedImageKey = "hpa",
                };
                horizontalPodAutoscalerNodes.Add(eachHorizontalPodAutoscalerTreeNode);
            }
            horizontalPodAutoscalersTreeNode.Nodes.AddRange(horizontalPodAutoscalerNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1HorizontalPodAutoscaler o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedHorizontalPodAutoscalerAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Horizontal Pod Autoscaler - {fetched.Metadata.Name}";
        }
    }
}
