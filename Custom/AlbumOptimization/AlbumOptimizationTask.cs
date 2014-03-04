using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        private int _itemsCount;

        private int _currentIndex;

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
            bool useCallbacks = _kConfig.UseCallbacks;
            bool useLossy = _kConfig.LossyOptimization;

            Kraken k = new Kraken(key, secret);

            Album album = _librariesManager.GetAlbum(this.AlbumId);

            var albumProvider = (LibrariesDataProvider)album.Provider;

            var images = album.Images().Where(i => i.Status == ContentLifecycleStatus.Master);

            _itemsCount = images.Count();

            foreach (Image image in images)
            {
                KrakenRequest kr = new KrakenRequest();

                kr.Lossy = useLossy;

                if (_kConfig.UseCallbacks)
                {
                    kr.CallbackUrl = _kConfig.CallbackURL;
                }
                else
                {
                    kr.Wait = true;
                }

                // Pull the Stream of the image from the provider.
                // This saves us from having to care about BlobStorage
                Stream imageData = albumProvider.Download(image);

                // Can't trust the length of Stream. Converting to a MemoryStream
                using (MemoryStream ms = new MemoryStream())
                {
                    imageData.CopyTo(ms);

                    kr.File = ms.ToArray();
                }

                var response = k.Upload(kr, image.Id.ToString(), image.Extension);

                if (_kConfig.UseCallbacks)
                {
                    Global.KrakenCallbackIds.Add(response.Id, album.Id);
                }
                else
                {
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

                        // Check to see if this image is already published.
                        // If it is, we need to publish the "Master" to update "Live"
                        if (image.GetWorkflowState() == "Published")
                        {
                            var bag = new Dictionary<string, string>();
                            bag.Add("ContentType", typeof(Image).FullName);
                            WorkflowManager.MessageWorkflow(image.Id, typeof(Image), albumProvider.Name, "Publish", false, bag);
                        }
                    }
                }

                UpdateProgress();
            }
        }

        private void UpdateProgress()
        {
            AlbumOptimizationTask albumOptimizationTask = this;
            albumOptimizationTask._currentIndex = albumOptimizationTask._currentIndex + 1;
            TaskProgressEventArgs taskProgressEventArg = new TaskProgressEventArgs()
            {
                Progress = this._currentIndex * 100 / this._itemsCount,
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