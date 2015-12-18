This is the project implementing the TFS OSLC Adapter

To run it, do the following:

- Create an ADAPTER_HOME structure, e.g.:
    C:\SomeDir\AdapterHomeDir\tfs

- Copy the following files into this folder:

    /com.ericsson.eif.tfs.oslc/config/adapter.properties
    /com.ericsson.eif.tfs.common/attribute_mapping.xml
    /com.ericsson.eif.tfs.common/customer_mapping.xml
    /com.ericsson.eif.tfs.common/product_mapping.xml

Edit the adapter.properties to set the correct properties:
- TFS server URL
- TFS functional username and password
- TFS SDK location
- TFS adapter username/password
NOTE: If exposing the TFS functional user credentials to clients, set the same
NOTE: as for the TFS functional user
- MHWeb link URLs:
    mhweb_related_cr_url
    mhweb_server_edit_url
    mhweb_base_edit_link_url (likely same as for test environment, as its a relative portion)

- Create a copy of the launch configuration "Launch TFS OSLC adapter"
and name it "My Launch TFS OSLC adapter" (a git ignore rule is in place which 
makes it easy to keep a personal copy). Then edit the value of the ADAPTER_HOME
on the "Environment" tab in the launch configuration so that it matches the 
structure you create above.

- Install the TFS Java SDK jar in the local repo for maven to build:
     cd <git repo>/com.ericsson.eif.tfs.oslc/lib/redist/lib
     mvn install:install-file -Dfile=com.microsoft.tfs.sdk-11.0.0.jar -DgroupId=com.ericsson.eif.tfs.lib -DartifactId=tfs-lib -Dversion=11.0.0 -Dpackaging=jar


