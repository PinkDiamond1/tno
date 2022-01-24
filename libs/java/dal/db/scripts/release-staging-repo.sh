#!/bin/bash

echo 'Enter the Staging Repository Id you want to release'
read -p 'Repository Id: cabcgovtno-' varStagingRepoId

if [ ! -z "$varStagingRepoId" ]
then
  cd dal-db
  export MAVEN_OPTS="--add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.desktop/java.awt.font=ALL-UNNAMED"
  
  if [ "$1" = "sudo" ]; then
    sudo env PATH="$PATH" mvn nexus-staging:release -P staging -DstagingRepositoryId="cabcgovtno-$varStagingRepoId" -s /home/vscode/.m2/settings.xml
  else
    mvn nexus-staging:release -P staging -DstagingRepositoryId="cabcgovtno-$varStagingRepoId" -s /home/vscode/.m2/settings.xml
  fi
fi
