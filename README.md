# dfc-providerportal-fileprocessor

Bulk file processing on Azure based on Blob storage triggers. 
Enables long-running file processing to be completed by the server without impacting browser response or causing an Azure timeout.

Eg. for Provider bulk course upload:
1. Provider uploads large CSV file of courses.
1. File is validated for "CSV-ness" and if invalid CSV response page displays error message.
1. If file is valid CSV then: if file is below certain (configurable) number of lines, process it inline otherwise process asynchronously.
1. If processing inline use the original code and workflow but upload the file with a ".processed" extension so that the Azure trigger ignores it.
1. If processing asynchronously then upload the file and show an information response page "thanks for your upload please check back later".
1. Azure trigger fires any time new file arrives in Blob container - checks if its a file to be processed and calls processing service.
1. Processing Service validates file for CSVness (don't rely on CSV validation being called from the UI we might have other front end clients in the future)
1. If file was invalid CSV, processing stops. (errors go where?).
1. If file was valid CSV process it as per the old code. Processing errors get attached to uploaded courses.

Threshold configuration is set by the BlobStorageSetting of "InlineProcessingThreshold" in the CourseDirectory project.

### Tech Debt

1. CourseDirectory code is being re-used by duplicating the code in to projects in the the "copied" solution folder - Dfc.CourseDirectory.Common, Dfc.CourseDirectory.Models, Dfc.CourseDirectory.Services. This needs DRYing out, perhaps in to Nuget packages.
1. The original code passed the user id to the upload from the logged in session - now that the processing is decoupled and running server there is no session to get a user id from so the Course "CreatedBy" field is set to "ProviderCsvFileImporter". Perhaps could tuck the user id in to the uploaded file name to be parsed out by the server if it's important to have the user id?
1. If CSV processing encounters errors with the file itself (eg. invalid format) the errors are logged but as far as the UI is concerned the process has failed silently. Need some kind of feedback.