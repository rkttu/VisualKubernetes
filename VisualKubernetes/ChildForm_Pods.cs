using k8s;
using k8s.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandPodResponse = System.Tuple<k8s.Models.V1Namespace, k8s.Models.V1PodList, System.Windows.Forms.TreeNode>;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    public sealed class ChildForm_Pods : ISubComponent<V1Pod>
    {
        public ChildForm_Pods(IWin32Window parentWindow, Func<string> text, Func<KubernetesClientConfiguration> k8sConfig, IContainer container)
        {
            this.parentWindow = parentWindow;
            this.text = text;
            this.k8sConfig = k8sConfig;
            this.container = container;
        }

        private readonly IWin32Window parentWindow;
        private readonly Func<string> text;
        private readonly Func<KubernetesClientConfiguration> k8sConfig;
        private readonly IContainer container;

        private BackgroundWorker podsLoader;

        public void Initialize()
        {
            podsLoader = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false,
            };
            podsLoader.DoWork += PodsLoader_DoWork;
            podsLoader.RunWorkerCompleted += PodsLoader_RunWorkerCompleted;
            container.Add(podsLoader);
        }

        private void PodsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as ExpandRequest;
            using var client = new Kubernetes(k8sConfig.Invoke());
            e.Result = new ExpandPodResponse(
                args.Item1,
                client.ListNamespacedPod(args.Item1.Metadata.Name),
                args.Item2);
        }

        private void PodsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show(parentWindow,
                    $"Cannot load pods due to error - {e.Error.Message}",
                    text.Invoke(), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            if (e.Cancelled)
            {
                MessageBox.Show(parentWindow,
                    "Cannot load pods due to user's cancellation request.",
                    text.Invoke(), MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                return;
            }

            var response = e.Result as ExpandPodResponse;
            var podList = response.Item2;
            if (podList == null)
            {
                MessageBox.Show(parentWindow,
                    "Cannot load pods due to error, but reason is unknown.",
                    text.Invoke(), MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                return;
            }

            var podsTreeNode = response.Item3;
            podsTreeNode.Nodes.Clear();
            var podNodes = new List<TreeNode>();
            for (int i = 0, max = podList.Items.Count; i < max; i++)
            {
                var eachPodTreeNode = new TreeNode(podList.Items[i].Metadata.Name)
                {
                    Tag = new NamespacedMetadata(response.Item1, podList.Items[i]),
                    ImageKey = "pod",
                    SelectedImageKey = "pod",
                };
                podNodes.Add(eachPodTreeNode);
            }
            podsTreeNode.Nodes.AddRange(podNodes.ToArray());
        }

        public async Task<string> DescribeObject(Kubernetes client, V1Namespace ns, V1Pod o, StringBuilder buffer)
        {
            var fetched = await client.ReadNamespacedPodAsync(o.Metadata.Name, ns.Metadata.Name).ConfigureAwait(false);
            buffer.AppendLine($"API Veresion: {fetched.ApiVersion}");
            buffer.AppendLine($"Kind: {fetched.Kind}");
            buffer.AppendLine(ChildForm.DescribeMetadata(fetched.Metadata));
            return $"Pod - {fetched.Metadata.Name}";
        }

        public void Run(ExpandRequest expandRequest)
        {
            if (podsLoader.IsBusy)
                return;

            podsLoader.RunWorkerAsync(expandRequest);
        }
    }
}
