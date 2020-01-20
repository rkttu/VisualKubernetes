using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandIngressResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.Extensionsv1beta1IngressList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker ingressesLoader;

        private void InitializeIngressLoader()
        {
            ingressesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            ingressesLoader.DoWork += IngressesLoader_DoWork;
            ingressesLoader.RunWorkerCompleted += IngressesLoader_RunWorkerCompleted;
            container.Add(ingressesLoader);
        }

        private void IngressesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandIngressResponse(
                args.Item1,
                client.ListNamespacedIngress(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void IngressesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load ingresses due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load ingresses due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandIngressResponse;
            var ingressList = response.Item2;
            if (ingressList == null)
            {
                MessageBox.Show(this,
                    "Cannot load ingresses due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var ingressesTreeNode = response.Item3;
            ingressesTreeNode.Nodes.Clear();
            var ingressNodes = new List<TreeNode>();
            for (int i = 0, max = ingressList.Items.Count; i < max; i++)
            {
                var eachIngressTreeNode = new TreeNode(ingressList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, ingressList.Items[i]),
                    ImageKey = "ing",
                    SelectedImageKey = "ing",
                };
                ingressNodes.Add(eachIngressTreeNode);
            }
            ingressesTreeNode.Nodes.AddRange(ingressNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, Extensionsv1beta1Ingress o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedIngressAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Ingress - {fetched.Metadata.Name}";
        }
    }
}
