using k8s;
using k8s.Models;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpandRequest = System.Tuple<k8s.Models.V1Namespace, System.Windows.Forms.TreeNode>;
using FetchResult = System.Tuple<string, string>;
using NamespacedMetadata = System.Tuple<k8s.Models.V1Namespace, object>;

namespace VisualKubernetes
{
    [DesignerCategory("")]
    public sealed partial class ChildForm : Form
    {
        public ChildForm(string kubeconfigFilePath)
        {
            KubeConfigFilePath = kubeconfigFilePath;
            InitializeComponents();
            InitializeFormDesign();
        }

        private IContainer container;
        private SplitContainer leftRightLayoutPanel;
        private TreeView itemView;
        private TabControl detailViewTabs;

        private ChildForm_ConfigMaps ChildForm_ConfigMaps;
        private ChildForm_Pods ChildForm_Pods;

        public string KubeConfigFilePath { get; }

        private void InitializeComponents()
        {
            container = new Container();

            InitializeImageList();

            InitializeConfigLoader();

            InitializeNamespaceLoader();
            InitializeNodeLoader();
            InitializeComponentStatusesLoader();
            InitializePersistentVolumesLoader();
            InitializeMutatingWebhookConfigurationLoader();
            InitializeValidatingWebhookConfigurationLoader();
            InitializeCustomResourceDefinitionLoader();
            InitializeAPIServiceLoader();
            InitializeCertificateSigningRequestLoader();
            InitializePodSecurityPolicyLoader();
            InitializeClusterRoleBindingLoader();
            InitializeClusterRoleLoader();
            InitializePriorityClassLoader();
            InitializeCSIDriverLoader();
            InitializeCSINodeLoader();
            InitializeStorageClassLoader();
            InitializeVolumeAttachmentLoader();
            ChildForm_ConfigMaps = new ChildForm_ConfigMaps(this, () => Text, () => k8sConfig, container);
            ChildForm_ConfigMaps.Initialize();
            InitializeEndpointLoader();
            InitializeLimitRangesLoader();
            InitializePersistentVolumeClaimsLoader();
            ChildForm_Pods = new ChildForm_Pods(this, () => Text, () => k8sConfig, container);
            ChildForm_Pods.Initialize();
            InitializePodTemplateLoader();
            InitializeReplicationControllerLoader();
            InitializeResourceQuotaLoader();
            InitializeSecretLoader();
            InitializeServiceAccountLoader();
            InitializeServiceLoader();

            InitializeControllerRevisionLoader();
            InitializeDaemonSetLoader();
            InitializeDeploymentLoader();
            InitializeReplicaSetLoader();
            InitializeStatefulSetLoader();

            InitializeHorizontalPodAutoscalerLoader();

            InitializeCronJobLoader();
            InitializeJobLoader();

            InitializeIngressLoader();

            InitializePodDisruptionBudgetLoader();

            InitializeRoleBindingLoader();
            InitializeRoleLoader();

            InitializeLeaseLoader();
            InitializeEventLoader();
            InitializeNetworkPolicyLoader();
        }

        private void InitializeFormDesign()
        {
            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);

            Load += ChildForm_Load;

            leftRightLayoutPanel = new SplitContainer()
            {
                Parent = this,
                SplitterDistance = 30,
                Orientation = Orientation.Vertical,
                Dock = DockStyle.Fill,
            };

            itemView = new TreeView()
            {
                Parent = leftRightLayoutPanel.Panel1,
                Dock = DockStyle.Fill,
                ImageList = treeViewImageList,
            };

            // http://dotnetrix.co.uk/tabcontrol.htm#tip7
            detailViewTabs = new TabControl()
            {
                Parent = leftRightLayoutPanel.Panel2,
                Dock = DockStyle.Fill,
                Multiline = true,
                Alignment = TabAlignment.Top,
                AllowDrop = true,
                DrawMode = TabDrawMode.OwnerDrawFixed,
            };
            detailViewTabs.MouseMove += DetailViewTabs_MouseMove;
            detailViewTabs.MouseDown += DetailViewTabs_MouseDown;
            detailViewTabs.DragOver += DetailViewTabs_DragOver;
            detailViewTabs.MouseUp += DetailViewTabs_MouseUp;
            detailViewTabs.DrawItem += DetailViewTabs_DrawItem;
            detailViewTabs.HandleCreated += DetailViewTabs_HandleCreated;
            
            ResumeLayout();
        }

        private void ItemView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            ProcessItemSelect(e.Node);
        }

        private void ItemView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var namespacedMetadata = e.Node?.Tag as NamespacedMetadata;
            var namespaceMetadata = namespacedMetadata?.Item1;
            var valueObject = namespacedMetadata?.Item2;
            OpenKubernetesObjectWindow(namespaceMetadata, valueObject);
        }

        private void ItemView_KeyPress(object sender, KeyPressEventArgs e)
        {
            var realSender = sender as TreeView;

            if (realSender == null)
                return;

            var namespacedMetadata = realSender.SelectedNode?.Tag as NamespacedMetadata;
            var namespaceMetadata = namespacedMetadata?.Item1;
            var valueObject = namespacedMetadata?.Item2;
            OpenKubernetesObjectWindow(namespaceMetadata, valueObject);
            e.Handled = true;
        }

        private void DetailViewTabs_HandleCreated(object sender, EventArgs e)
        {
            var realSender = (TabControl)sender;
            NativeMethods.SendMessage(
                realSender.Handle,
                NativeMethods.TCM_SETMINTABWIDTH,
                IntPtr.Zero, (IntPtr)16);
        }

        // https://social.technet.microsoft.com/wiki/contents/articles/50957.c-winform-tabcontrol-with-add-and-close-button.aspx
        private void DetailViewTabs_DrawItem(object sender, DrawItemEventArgs e)
        {
            try
            {
                var realSender = (TabControl)sender;
                var tabPage = realSender.TabPages[e.Index];
                var tabRect = realSender.GetTabRect(e.Index);
                tabRect.Inflate(-2, -2);

                var closeImage = toolBarImageList.Images["close"];
                e.Graphics.DrawImage(closeImage,
                    (tabRect.Right - closeImage.Width),
                    tabRect.Top + (tabRect.Height - closeImage.Height) / 2);
                TextRenderer.DrawText(e.Graphics, tabPage.Text, tabPage.Font,
                    tabRect, tabPage.ForeColor, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            }
            catch (Exception ex) { throw new Exception(ex.Message); }
        }

        private async void OpenKubernetesObjectWindow(V1Namespace namespaceMetadata, object hint)
        {
            IKubernetesObject kubernetesObject = hint as IKubernetesObject;

            if (string.Equals(hint as string, "ns", StringComparison.OrdinalIgnoreCase))
                kubernetesObject = namespaceMetadata;
            
            if (kubernetesObject == null)
                return;

            var targetPage = default(TabPage);
            for (int i = 0; i < detailViewTabs.TabCount; i++)
            {
                if (object.ReferenceEquals(detailViewTabs.TabPages[i].Tag, kubernetesObject))
                {
                    targetPage = detailViewTabs.TabPages[i];
                    break;
                }
            }

            FetchResult content;
            try { content = await OpenKubernetesObjectInternal(namespaceMetadata, kubernetesObject); }
            catch (Exception ex)
            {
                content = new FetchResult("Error", ex.ToString());
            }

            if (targetPage == null)
            {
                targetPage = new TabPage(kubernetesObject.GetType().ToString())
                {
                    Tag = kubernetesObject,
                    Text = content.Item1,
                };
                targetPage.Controls.Add(new RichTextBox()
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    Text = content.Item2,
                    MaxLength = Int32.MaxValue,
                    WordWrap = false,
                    AutoWordSelection = true,
                    HideSelection = false,
                    ScrollBars = RichTextBoxScrollBars.ForcedBoth,
                    Font = new Font("Consolas", 12f),
                });
                detailViewTabs.TabPages.Add(targetPage);
            }

            // TODO: Prevent focus when change visible tab
            detailViewTabs.SelectedTab = targetPage;
        }

        private async Task<FetchResult> OpenKubernetesObjectInternal(V1Namespace namespaceMetadata, IKubernetesObject kubernetesObject)
        {
            using var client = new Kubernetes(k8sConfig);
            var buffer = new StringBuilder();
            var title = "(Unknown)";

            if (namespaceMetadata == null)
                buffer.AppendLine($"Namespace: (Global)");
            else
                buffer.AppendLine($"Namespace: {namespaceMetadata.Metadata.Name}");

            buffer.AppendLine($"Object Type: {kubernetesObject.GetType()}");

            switch (kubernetesObject)
            {
                case V1Namespace ns:
                    title = await DescribeObject(client, ns, buffer).ConfigureAwait(false);
                    break;

                case V1ComponentStatus cs:
                    title = await DescribeObject(client, cs, buffer).ConfigureAwait(false);
                    break;

                case V1Node node:
                    title = await DescribeObject(client, node, buffer).ConfigureAwait(false);
                    break;

                case V1PersistentVolume pv:
                    title = await DescribeObject(client, pv, buffer).ConfigureAwait(false);
                    break;

                case V1MutatingWebhookConfiguration mutatingWebhookConfiguration:
                    title = await DescribeObject(client, mutatingWebhookConfiguration, buffer).ConfigureAwait(false);
                    break;

                case V1ValidatingWebhookConfiguration validatingWebhookConfiguration:
                    title = await DescribeObject(client, validatingWebhookConfiguration, buffer).ConfigureAwait(false);
                    break;

                case V1CustomResourceDefinition crds:
                    title = await DescribeObject(client, crds, buffer).ConfigureAwait(false);
                    break;

                case V1APIService apiService:
                    title = await DescribeObject(client, apiService, buffer).ConfigureAwait(false);
                    break;

                case V1beta1CertificateSigningRequest csr:
                    title = await DescribeObject(client, csr, buffer).ConfigureAwait(false);
                    break;

                case Extensionsv1beta1PodSecurityPolicy psp:
                    title = await DescribeObject(client, psp, buffer).ConfigureAwait(false);
                    break;

                case V1ClusterRoleBinding clusterrolebinding:
                    title = await DescribeObject(client, clusterrolebinding, buffer).ConfigureAwait(false);
                    break;

                case V1ClusterRole clusterrole:
                    title = await DescribeObject(client, clusterrole, buffer).ConfigureAwait(false);
                    break;

                case V1PriorityClass pc:
                    title = await DescribeObject(client, pc, buffer).ConfigureAwait(false);
                    break;

                case V1beta1CSIDriver csidriver:
                    title = await DescribeObject(client, csidriver, buffer).ConfigureAwait(false);
                    break;

                case V1beta1CSINode csinode:
                    title = await DescribeObject(client, csinode, buffer).ConfigureAwait(false);
                    break;

                case V1StorageClass sc:
                    title = await DescribeObject(client, sc, buffer).ConfigureAwait(false);
                    break;

                case V1VolumeAttachment volumeattachment:
                    title = await DescribeObject(client, volumeattachment, buffer).ConfigureAwait(false);
                    break;

                case V1ConfigMap cm:
                    title = await ChildForm_ConfigMaps.DescribeObject(client, namespaceMetadata, cm, buffer).ConfigureAwait(false);
                    break;

                case V1ControllerRevision ctrlrev:
                    title = await DescribeObject(client, namespaceMetadata, ctrlrev, buffer).ConfigureAwait(false);
                    break;

                case V1DaemonSet ds:
                    title = await DescribeObject(client, namespaceMetadata, ds, buffer).ConfigureAwait(false);
                    break;

                case V1Deployment deploy:
                    title = await DescribeObject(client, namespaceMetadata, deploy, buffer).ConfigureAwait(false);
                    break;

                case V1Endpoints ep:
                    title = await DescribeObject(client, namespaceMetadata, ep, buffer).ConfigureAwait(false);
                    break;

                case V1LimitRange limit:
                    title = await DescribeObject(client, namespaceMetadata, limit, buffer).ConfigureAwait(false);
                    break;

                case V1PersistentVolumeClaim pvc:
                    title = await DescribeObject(client, namespaceMetadata, pvc, buffer).ConfigureAwait(false);
                    break;

                case V1Pod pod:
                    title = await ChildForm_Pods.DescribeObject(client, namespaceMetadata, pod, buffer).ConfigureAwait(false);
                    break;

                case V1PodTemplate podTemplate:
                    title = await DescribeObject(client, namespaceMetadata, podTemplate, buffer).ConfigureAwait(false);
                    break;

                case V1ReplicationController replCtrl:
                    title = await DescribeObject(client, namespaceMetadata, replCtrl, buffer).ConfigureAwait(false);
                    break;

                case V1ResourceQuota quota:
                    title = await DescribeObject(client, namespaceMetadata, quota, buffer).ConfigureAwait(false);
                    break;

                case V1Secret secret:
                    title = await DescribeObject(client, namespaceMetadata, secret, buffer).ConfigureAwait(false);
                    break;

                case V1ServiceAccount sa:
                    title = await DescribeObject(client, namespaceMetadata, sa, buffer).ConfigureAwait(false);
                    break;

                case V1Service svc:
                    title = await DescribeObject(client, namespaceMetadata, svc, buffer).ConfigureAwait(false);
                    break;

                case V1ReplicaSet rs:
                    title = await DescribeObject(client, namespaceMetadata, rs, buffer).ConfigureAwait(false);
                    break;

                case V1StatefulSet sts:
                    title = await DescribeObject(client, namespaceMetadata, sts, buffer).ConfigureAwait(false);
                    break;

                case V1HorizontalPodAutoscaler hpa:
                    title = await DescribeObject(client, namespaceMetadata, hpa, buffer).ConfigureAwait(false);
                    break;

                case V1beta1CronJob cj:
                    title = await DescribeObject(client, namespaceMetadata, cj, buffer).ConfigureAwait(false);
                    break;

                case V1Job job:
                    title = await DescribeObject(client, namespaceMetadata, job, buffer).ConfigureAwait(false);
                    break;

                case Extensionsv1beta1Ingress ing:
                    title = await DescribeObject(client, namespaceMetadata, ing, buffer).ConfigureAwait(false);
                    break;

                case V1beta1PodDisruptionBudget pdb:
                    title = await DescribeObject(client, namespaceMetadata, pdb, buffer).ConfigureAwait(false);
                    break;

                case V1RoleBinding rolebinding:
                    title = await DescribeObject(client, namespaceMetadata, rolebinding, buffer).ConfigureAwait(false);
                    break;

                case V1Role role:
                    title = await DescribeObject(client, namespaceMetadata, role, buffer).ConfigureAwait(false);
                    break;

                case V1Lease lease:
                    title = await DescribeObject(client, namespaceMetadata, lease, buffer).ConfigureAwait(false);
                    break;

                case V1Event @event:
                    title = await DescribeObject(client, namespaceMetadata, @event, buffer).ConfigureAwait(false);
                    break;

                case V1beta1NetworkPolicy netpol:
                    title = await DescribeObject(client, namespaceMetadata, netpol, buffer).ConfigureAwait(false);
                    break;

                default:
                    break;
            }

            return new FetchResult(title, buffer.ToString());
        }

        // TODO: Convert to TPL
        private void ProcessItemSelect(TreeNode node)
        {
            if (k8sConfig == null)
                return;

            var namespacedMetadata = node?.Tag as NamespacedMetadata;
            var namespaceMetadata = namespacedMetadata?.Item1;

            if (node.Nodes.Count > 0)
                return;

            if (namespaceMetadata == null)
            {
                switch (node.Tag as string)
                {
                    case "ns":
                        if (namespacesLoader.IsBusy)
                            return;

                        namespacesLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "node":
                        if (nodesLoader.IsBusy)
                            return;

                        nodesLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "cs":
                        if (componentStatusesLoader.IsBusy)
                            return;

                        componentStatusesLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "pv":
                        if (persistentVolumesLoader.IsBusy)
                            return;

                        persistentVolumesLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "mutatingwebhookconfiguration":
                        if (mutatingWebhookConfigurationsLoader.IsBusy)
                            return;

                        mutatingWebhookConfigurationsLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "validatingwebhookconfiguration":
                        if (validatingWebhookConfigurationsLoader.IsBusy)
                            return;

                        validatingWebhookConfigurationsLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "crds":
                        if (customResourceDefinitionsLoader.IsBusy)
                            return;

                        customResourceDefinitionsLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "apiservice":
                        if (apiServicesLoader.IsBusy)
                            return;

                        apiServicesLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "csr":
                        if (certificateSigningRequestsLoader.IsBusy)
                            return;

                        certificateSigningRequestsLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "psp":
                        if (podSecurityPoliciesLoader.IsBusy)
                            return;

                        podSecurityPoliciesLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "clusterrolebinding":
                        if (clusterRoleBindingsLoader.IsBusy)
                            return;

                        clusterRoleBindingsLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "pc":
                        if (priorityClassesLoader.IsBusy)
                            return;

                        priorityClassesLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "csidriver":
                        if (csiDriversLoader.IsBusy)
                            return;

                        csiDriversLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "csinode":
                        if (csiNodesLoader.IsBusy)
                            return;

                        csiNodesLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "sc":
                        if (storageClassesLoader.IsBusy)
                            return;

                        storageClassesLoader.RunWorkerAsync(k8sConfig);
                        break;

                    case "volumeattachment":
                        if (volumeAttachmentsLoader.IsBusy)
                            return;

                        volumeAttachmentsLoader.RunWorkerAsync(k8sConfig);
                        break;

                    default:
                        return;
                }
            }
            else
            {
                switch (namespacedMetadata.Item2)
                {
                    case "pod":
                        ChildForm_Pods.Run(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "svc":
                        if (servicesLoader.IsBusy)
                            return;

                        servicesLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "cm":
                        ChildForm_ConfigMaps.Run(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "ep":
                        if (endpointsLoader.IsBusy)
                            return;

                        endpointsLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "limits":
                        if (limitRangesLoader.IsBusy)
                            return;

                        limitRangesLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "pvc":
                        if (persistentVolumeClaimsLoader.IsBusy)
                            return;

                        persistentVolumeClaimsLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "podtemplates":
                        if (podTemplatesLoader.IsBusy)
                            return;

                        podTemplatesLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "rc":
                        if (replicationControllersLoader.IsBusy)
                            return;

                        replicationControllersLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "quota":
                        if (resourceQuotasLoader.IsBusy)
                            return;

                        resourceQuotasLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "sa":
                        if (serviceAccountsLoader.IsBusy)
                            return;

                        serviceAccountsLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "controllerrevisions":
                        if (controllerRevisionsLoader.IsBusy)
                            return;

                        controllerRevisionsLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "ds":
                        if (daemonSetsLoader.IsBusy)
                            return;

                        daemonSetsLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "deploy":
                        if (deploymentsLoader.IsBusy)
                            return;

                        deploymentsLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "rs":
                        if (replicaSetsLoader.IsBusy)
                            return;

                        replicaSetsLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "sts":
                        if (statefulSetsLoader.IsBusy)
                            return;

                        statefulSetsLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "hpa":
                        if (horizontalPodAutoscalersLoader.IsBusy)
                            return;

                        horizontalPodAutoscalersLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "cj":
                        if (cronJobsLoader.IsBusy)
                            return;

                        cronJobsLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "job":
                        if (jobsLoader.IsBusy)
                            return;

                        jobsLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "ing":
                        if (ingressesLoader.IsBusy)
                            return;

                        ingressesLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "pdb":
                        if (podDisruptionBudgetsLoader.IsBusy)
                            return;

                        podDisruptionBudgetsLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "lease":
                        if (leasesLoader.IsBusy)
                            return;

                        leasesLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "event":
                        if (eventsLoader.IsBusy)
                            return;

                        eventsLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "netpol":
                        if (networkPoliciesLoader.IsBusy)
                            return;

                        networkPoliciesLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "rolebinding":
                        if (roleBindingsLoader.IsBusy)
                            return;

                        roleBindingsLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    case "role":
                        if (rolesLoader.IsBusy)
                            return;

                        rolesLoader.RunWorkerAsync(new ExpandRequest(namespaceMetadata, node));
                        break;

                    default:
                        return;
                }
            }

            return;
        }

        private void ChildForm_Load(object sender, EventArgs e)
        {
            try
            {
                configLoader.RunWorkerAsync(KubeConfigFilePath);
            }
            catch (Exception thrownException)
            {
                MessageBox.Show(this,
                    $"Cannot load namespaces due to error - {thrownException.Message}",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                Close();
                return;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (container != null)
                {
                    container.Dispose();
                    container = null;
                }
            }

            base.Dispose(disposing);
        }

        private void DetailViewTabs_MouseDown(object sender, MouseEventArgs e)
        {
            var realSender = (TabControl)sender;

            // Process MouseDown event only till (tabControl.TabPages.Count) excluding the last TabPage
            for (var i = 0; i < realSender.TabPages.Count; i++)
            {
                var tabRect = realSender.GetTabRect(i);
                tabRect.Inflate(-2, -2);
                var closeImage = this.toolBarImageList.Images["close"];
                var imageRect = new Rectangle(
                    (tabRect.Right - closeImage.Width),
                    tabRect.Top + (tabRect.Height - closeImage.Height) / 2,
                    closeImage.Width,
                    closeImage.Height);

                if (imageRect.Contains(e.Location))
                {
                    realSender.TabPages.RemoveAt(i);
                    break;
                }
            }

            // store clicked tab
            var hoverIndex = GetHoverTabIndex(realSender);

            if (hoverIndex >= 0)
                realSender.Tag = realSender.TabPages[hoverIndex];
        }

        private void DetailViewTabs_MouseUp(object sender, MouseEventArgs e)
        {
            // clear stored tab
            var realSender = (TabControl)sender;
            realSender.Tag = null;
        }

        private void DetailViewTabs_MouseMove(object sender, MouseEventArgs e)
        {
            // mouse button down? tab was clicked?
            var realSender = (TabControl)sender;
            if ((e.Button != MouseButtons.Left) || (realSender.Tag == null))
                return;

            var clickedTab = (TabPage)realSender.Tag;
            _ = realSender.TabPages.IndexOf(clickedTab);

            // start drag n drop
            realSender.DoDragDrop(clickedTab, DragDropEffects.All);
        }

        private void DetailViewTabs_DragOver(object sender, DragEventArgs e)
        {
            TabControl realSender = (TabControl)sender;

            // a tab is draged?
            if (e.Data.GetData(typeof(TabPage)) == null)
                return;

            var dragTab = (TabPage)e.Data.GetData(typeof(TabPage));
            var dragTabIndex = realSender.TabPages.IndexOf(dragTab);

            // hover over a tab?
            var hoverTabIndex = this.GetHoverTabIndex(realSender);
            if (hoverTabIndex < 0)
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            var hoverTab = realSender.TabPages[hoverTabIndex];
            e.Effect = DragDropEffects.Move;

            // start of drag?
            if (object.ReferenceEquals(dragTab, hoverTab))
                return;

            // swap dragTab & hoverTab - avoids toggeling
            var dragTabRect = realSender.GetTabRect(dragTabIndex);
            var hoverTabRect = realSender.GetTabRect(hoverTabIndex);

            if (dragTabRect.Width < hoverTabRect.Width)
            {
                var tcLocation = realSender.PointToScreen(realSender.Location);

                if (dragTabIndex < hoverTabIndex)
                {
                    if ((e.X - tcLocation.X) > ((hoverTabRect.X + hoverTabRect.Width) - dragTabRect.Width))
                        SwapTabPages(realSender, dragTab, hoverTab);
                }
                else if (dragTabIndex > hoverTabIndex)
                {
                    if ((e.X - tcLocation.X) < (hoverTabRect.X + dragTabRect.Width))
                        SwapTabPages(realSender, dragTab, hoverTab);
                }
            }
            else SwapTabPages(realSender, dragTab, hoverTab);

            // select new pos of dragTab
            realSender.SelectedIndex = realSender.TabPages.IndexOf(dragTab);
        }

        private int GetHoverTabIndex(TabControl realSender)
        {
            for (var i = 0; i < realSender.TabPages.Count; i++)
            {
                if (realSender.GetTabRect(i).Contains(realSender.PointToClient(Cursor.Position)))
                    return i;
            }

            return (-1);
        }

        private void SwapTabPages(TabControl realSender, TabPage sourcePage, TabPage destPage)
        {
            int srcIndex = realSender.TabPages.IndexOf(sourcePage);
            int dstIndex = realSender.TabPages.IndexOf(destPage);
            realSender.TabPages[dstIndex] = sourcePage;
            realSender.TabPages[srcIndex] = destPage;
            realSender.Refresh();
        }

        public static string DescribeMetadata(V1ObjectMeta objectMeta)
        {
            var buffer = new StringBuilder();
            buffer.AppendLine("Metadata: ");
            buffer.AppendLine($"- Name: {objectMeta.Name}");
            return buffer.ToString();
        }
    }
}
