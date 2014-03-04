using SitefinityWebApp.Custom.AlbumOptimization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using Telerik.Sitefinity.Libraries.Model;
using Telerik.Sitefinity.Modules.Libraries;
using Telerik.Sitefinity.Scheduling;
using Telerik.Sitefinity.Workflow;

namespace SitefinityWebApp.Mvc.Controllers
{
    public class OptimizationController : ApiController
    {
        [HttpPost]
        public HttpResponseMessage Post([FromBody] FormDataCollection kCallbackFormData)
        {
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);

            if (Global.KrakenCallbackIds.ContainsKey(kCallbackFormData.Get("Id")))
            {
                ProcessCallback(kCallbackFormData);

                response.StatusCode = HttpStatusCode.OK;
            }

            response.StatusCode = HttpStatusCode.Forbidden;

            return response;
        }

        [Authorize]
        [HttpPost]
        public Guid Post(Guid id)
        {
            return StartOptimizeAlbumItemsTask(id);
        }

        private Guid StartOptimizeAlbumItemsTask(Guid albumId)
        {
            SchedulingManager manager = SchedulingManager.GetManager();

            Guid guid = Guid.NewGuid();

            AlbumOptimizationTask albumOptimizationTask = new AlbumOptimizationTask()
            {
                Id = guid,
                AlbumId = albumId
            };

            manager.AddTask(albumOptimizationTask);

            manager.SaveChanges();

            return guid;
        }

        private void ProcessCallback(FormDataCollection kCallbackFormData)
        {
            string krakenCallbackId = kCallbackFormData.Get("id");
            string fileName = kCallbackFormData.Get("file_name");
            string krakedUrl = kCallbackFormData.Get("kraked_url");
            bool success = Boolean.Parse(kCallbackFormData.Get("success"));
            string error = kCallbackFormData.Get("error");

            Guid albumId = Global.KrakenCallbackIds[krakenCallbackId];

            Global.KrakenCallbackIds.Remove(krakenCallbackId);

            if (success == false || error != null)
            {
                return;
            }

            LibrariesManager _librariesManager = LibrariesManager.GetManager();

            _librariesManager.Provider.SuppressSecurityChecks = true;

            Album album = _librariesManager.GetAlbum(albumId);

            var albumProvider = (LibrariesDataProvider)album.Provider;

            Image image = _librariesManager.GetImage(Guid.Parse(Path.GetFileNameWithoutExtension(krakedUrl)));

            if (!Regex.IsMatch(krakedUrl, @"https?://(?:api-worker-\d|dl).kraken.io/" + krakenCallbackId + "/" + image.Id.ToString() + @"\.(?:jpg|jpeg|png|gif|svg)"))
            {
                return;
            }

            using (var webClient = new WebClient())
            using (var stream = webClient.OpenRead(krakedUrl))
            {
                //Check out the master to get a temp version.
                Image temp = _librariesManager.Lifecycle.CheckOut(image) as Image;

                //Make the modifications to the temp version.
                _librariesManager.Upload(temp, stream, Path.GetExtension(fileName));

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

            _librariesManager.Provider.SuppressSecurityChecks = false;
        }
    }
}