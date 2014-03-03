using Newtonsoft.Json;
using SitefinityWebApp.Custom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using Telerik.Sitefinity.Configuration;
using Telerik.Sitefinity.GenericContent.Model;
using Telerik.Sitefinity.Libraries.Model;
using Telerik.Sitefinity.Modules.Libraries;
using Telerik.Sitefinity.Scheduling;
using Telerik.Sitefinity.Workflow;

namespace SitefinityWebApp.Custom.AlbumOptimization
{
    public class AlbumOptimizationTask : ScheduledTask
    {
        private int itemsCount;

        private int currentIndex;

        public Guid AlbumId
        {
            get;
            set;
        }

        public override string TaskName
        {
            get
            {
                return "SitefinityWebApp.Custom.AlbumOptimization.AlbumOptimizationTask";
            }
        }

        public AlbumOptimizationTask()
        {
            Title = "OptimizeAlbum";
            ExecuteTime = DateTime.UtcNow;
            Description = "Optimizing images";
        }

        public override string BuildUniqueKey()
        {
            return this.GetCustomData();
        }

        public override void ExecuteTask()
        {
            KrakenConfig _kConfig = Config.Get<KrakenConfig>();
            LibrariesManager _librariesManager = LibrariesManager.GetManager();

            string key = _kConfig.APIKey;
            string secret = _kConfig.APISecret;
            bool lossy = _kConfig.LossyOptimization;

            Kraken k = new Kraken(key, secret);

            Album album = _librariesManager.GetAlbum(this.AlbumId);

            var images = album.Images().Where(i => i.Status == ContentLifecycleStatus.Master);

            itemsCount = images.Count();

            foreach (Image image in images)
            {
                var albumProvider = (LibrariesDataProvider)album.Provider;

                KrakenRequest kr = new KrakenRequest();

                kr.Lossy = lossy;
                kr.Wait = true;

                Stream imageData = albumProvider.Download(image);

                using (var br = new BinaryReader(imageData))
                {
                    kr.File = br.ReadBytes((int)imageData.Length);
                }

                var response = k.Upload(kr, Path.GetFileNameWithoutExtension(image.FilePath), image.Extension);

                if (response.Success == false || response.Error != null)
                {
                    UpdateProgress();
                    continue;
                }

                using (var webClient = new WebClient())
                using (var stream = webClient.OpenRead(response.KrakedUrl))
                {
                    //Check out the master to get a temp version.
                    Image temp = _librariesManager.Lifecycle.CheckOut(image) as Image;

                    //Make the modifications to the temp version.
                    _librariesManager.Upload(temp, stream, Path.GetExtension(response.KrakedUrl));

                    //Checkin the temp and get the updated master version.
                    //After the check in the temp version is deleted.
                    _librariesManager.Lifecycle.CheckIn(temp);

                    _librariesManager.SaveChanges();

                    //Publish the image.
                    var bag = new Dictionary<string, string>();
                    bag.Add("ContentType", typeof(Image).FullName);
                    WorkflowManager.MessageWorkflow(image.Id, typeof(Image), albumProvider.Name, "Publish", false, bag);
                }

                UpdateProgress();
            }
        }

        private void UpdateProgress()
        {
            AlbumOptimizationTask albumOptimizationTask = this;
            albumOptimizationTask.currentIndex = albumOptimizationTask.currentIndex + 1;
            TaskProgressEventArgs taskProgressEventArg = new TaskProgressEventArgs()
            {
                Progress = this.currentIndex * 100 / this.itemsCount,
                StatusMessage = ""
            };
            TaskProgressEventArgs taskProgressEventArg1 = taskProgressEventArg;
            this.OnProgressChanged(taskProgressEventArg1);
            if (taskProgressEventArg1.Stopped)
            {
                throw new TaskStoppedException();
            }
        }

        public override string GetCustomData()
        {
            AlbumOptimizationTaskState albumOptimizationTaskState = new AlbumOptimizationTaskState(this);
            return JsonConvert.SerializeObject(albumOptimizationTaskState);
        }

        private void PersistState()
        {
            if (base.Id != Guid.Empty)
            {
                SchedulingManager schedulingManager = new SchedulingManager();
                this.CopyToTaskData(schedulingManager.GetTaskData(base.Id));
                schedulingManager.SaveChanges();
            }
        }

        public override void SetCustomData(string customData)
        {
            AlbumOptimizationTaskState albumOptimizationTaskState = JsonConvert.DeserializeObject<AlbumOptimizationTaskState>(customData);
            this.AlbumId = albumOptimizationTaskState.AlbumId;
        }
    }

    internal class AlbumOptimizationTaskState
    {
        public Guid AlbumId
        {
            get;
            set;
        }

        public AlbumOptimizationTaskState()
        {
        }

        public AlbumOptimizationTaskState(AlbumOptimizationTask albumOptimizationTask)
        {
            this.AlbumId = albumOptimizationTask.AlbumId;
        }
    }
}