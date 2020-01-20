using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandEndpointResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1EndpointsList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker endpointsLoader;

        private void InitializeEndpointLoader()
        {
            endpointsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            endpointsLoader.DoWork += EndpointsLoader_DoWork;
            endpointsLoader.RunWorkerCompleted += EndpointsLoader_RunWorkerCompleted;
            container.Add(endpointsLoader);
        }

        private void EndpointsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandEndpointResponse(
                args.Item1,
                client.ListNamespacedEndpoints(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void EndpointsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load endpoints due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load endpoints due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandEndpointResponse;
            var endpointList = response.Item2;
            if (endpointList == null)
            {
                MessageBox.Show(this,
                    "Cannot load endpoints due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var endpointsTreeNode = response.Item3;
            endpointsTreeNode.Nodes.Clear();
            var endpointNodes = new List<TreeNode>();
            for (int i = 0, max = endpointList.Items.Count; i < max; i++)
            {
                var eachEndpointTreeNode = new TreeNode(endpointList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, endpointList.Items[i]),
                    ImageKey = "ep",
                    SelectedImageKey = "ep",
                };
                endpointNodes.Add(eachEndpointTreeNode);
            }
            endpointsTreeNode.Nodes.AddRange(endpointNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1Endpoints o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedEndpointsAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Endpoint - {fetched.Metadata.Name}";
        }
    }
}
