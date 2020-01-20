using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandPodTemplateResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1PodTemplateList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    partial class ChildForm
    {
        private BackgroundWorker podTemplatesLoader;

        private void InitializePodTemplateLoader()
        {
            podTemplatesLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            podTemplatesLoader.DoWork += PodTemplatesLoader_DoWork;
            podTemplatesLoader.RunWorkerCompleted += PodTemplatesLoader_RunWorkerCompleted;
            container.Add(podTemplatesLoader);
        }

        private void PodTemplatesLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig);
            e.Result = new ExpandPodTemplateResponse(
                args.Item1,
                client.ListNamespacedPodTemplate(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void PodTemplatesLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(this,
                    $"Cannot load pod templates due to error - {e.Error.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(this,
                    "Cannot load pod templates due to user's cancellation request.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandPodTemplateResponse;
            var podTemplateList = response.Item2;
            if (podTemplateList == null)
            {
                MessageBox.Show(this,
                    "Cannot load pod templates due to error, but reason is unknown.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var podTemplatesTreeNode = response.Item3;
            podTemplatesTreeNode.Nodes.Clear();
            var podTemplateNodes = new List<TreeNode>();
            for (int i = 0, max = podTemplateList.Items.Count; i < max; i++)
            {
                var eachPodTemplateTreeNode = new TreeNode(podTemplateList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, podTemplateList.Items[i]),
                    ImageKey = "podtemplate",
                    SelectedImageKey = "podtemplate",
                };
                podTemplateNodes.Add(eachPodTemplateTreeNode);
            }
            podTemplatesTreeNode.Nodes.AddRange(podTemplateNodes.ToArray());
        }

        private async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1PodTemplate o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedPodTemplateAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(DescribeMetadata(fetched.Metadata));
            return $"Pod Template - {fetched.Metadata.Name}";
        }
    }
}
