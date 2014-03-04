#SitefinityKrakenIO

This is not a standalone project.

The files in this repository are an example of how to integrate Telerik Sitefinity CMS and Kraken.IO. You'll want to make sure you don't overwrite anything you already have going in your project. _(I'm specifically talking about what's in the Global.asax.cs)_

This customization adds the ability to batch optimize entire Sitefinity albums using [Kraken.io](https://kraken.io/).

##Dependencies

* Json.NET
* RestSharp

If you'd like, you can remove the dependencies by modifying the way Kraken.cs and AlbumOptimizationTask.cs handle the HTTPy/JSONy bits.

##How to hook everything up

###Copying the source files

Merge the source files into your project.

**Note:** If you have to modify the namespaces for any reason, make sure that `TaskName` in the AlbumOptimizationTask.cs gets updated to reflect the changes. Otherwise, Sitefinity won't know the correct class to instantiate.

###Adding the "Optimize" command to the Action menu

Navigate to:
**Administration -> Settings -> Advanced -> Libraries -> Controls -> AlbumsBackend -> Views -> AlbumsBackendList -> ViewModes -> Grid -> Columns -> Actions**

Once you get there, put the following HTML in the **Client Template** field:

_(This is the stock Client Template with the "Optimize" command already added to it.)_

```HTML
<ul id='actions' class='actionsMenu'>
	<li><a menu='actions' href='javascript:void(0);'>{$LibrariesResources,Actions$}</a>
		<ul>
			<li><a sys:href='javascript:void(0);' class='sf_binderCommand_delete sfDeleteItm'>{$LibrariesResources, Delete$}</a></li>
			<li><a sys:href='javascript:void(0);' class='sf_binderCommand_moveToSingle'>{$LibrariesResources, MoveTo$}</a></li>
			<li class='sfSeparator'></li>
			<li><a sys:href='javascript:void(0);' class='sf_binderCommand_optimize'>Optimize</a></li>
			<li class='sfSeparator'></li>
			<li><a sys:href='javascript:void(0);' class='sf_binderCommand_relocateLibrary'>{$LibrariesResources, RelocateLibrary$}</a></li>
			<li><a sys:href='javascript:void(0);' class='sf_binderCommand_transferLibrary'>{$LibrariesResources, TransferLibrary$}</a></li>
			<li><a sys:href='javascript:void(0);' class='sf_binderCommand_thumbnailSettings'>{$LibrariesResources, ThumbnailSettings$}</a></li>
			<li><a sys:href='javascript:void(0);' class='sf_binderCommand_regenerateThumbnails'>{$LibrariesResources, RegenerateThumbnails$}</a></li>
			<li class='sfSeparator'></li>
			<li><a sys:href='javascript:void(0);' class='sf_binderCommand_permissions'>{$LibrariesResources, SetPermissions$}</a></li>
			<li><a sys:href='javascript:void(0);' class='sf_binderCommand_edit'>{$LibrariesResources, EditProperties$}</a></li>
		</ul>
	</li>
</ul>
<div id='taskInfo' class="sfTaskInfoWrp">
	<span id='taskDescription'></span>
	<div id='errorDetailsPanel' class="sfDetailsPopupWrp sfInlineBlock" style='display:none'>
		<a id='errorDetailsLink' sys:href='javascript:void(0);' class="sfMoreDetails">{$Labels, Details$}</a>
		<div id='errorDetailsMessage' style='display:none' class="sfDetailsPopup sfFailDetails"></div>
	</div>
	<a id='taskCommand' sys:href='javascript:void(0);'></a>
	<div class="sfMoveProgressWrp">
		<div id='taskProgressBar' class="sfProgressBarWrp sfInlineBlock"></div>
		<span id='taskProgressReport' class="sfProgressPercentage"></span>
	</div>
</div>
<div id="tmbRegenNeeded" class="sfEmphazie" style='display:none'> {$LibrariesResources, ThumbnailsNeedRegeneration$}</div>
```

###Registering the backend script

Navigate to the **Scripts** section of the **AlbumsBackendList** configuration and then click "Create new".

Here are the values to register the script:

**Script location:** ~/Custom/AlbumOptimization/AlbumExtensions/AlbumExtensions.js

**Name of the load method:** albumsListLoaded


###Once everything is hooked up
* Build
* Head over to [Kraken.io](https://kraken.io/) and register for an account.
* Enter your API Key and API Secret in the new "Kraken" configuration section. _(Administration -> Settings -> Advanced -> Kraken)_

**Note:** By default **Lossy optimization** is turned off but it's worth turning on if you want to really optimize the images. Check out [the docs](https://kraken.io/docs/lossy-optimization) to see if it's something you're interested in.