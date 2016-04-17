using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class SectionPosition {
  public int startLine = -1;
  public int endLine = -1;
  public int startCharIndex = -1;
  public int endCharIndex = -1;

  public SectionPosition(int newStartLine, int newStartCharIndex) {
    startLine = newStartLine;
    startCharIndex = newStartCharIndex;
  }

  public override string ToString() {
    return "Start Line: " + startLine +  " StartCharIndex: " + startCharIndex + "\nEnd Line: " + endLine + " EndCharIndex: " + endCharIndex;
  }
}
//-----------------------------------------
public class Section {
  public Section parentSection = null;
  public List<SectionPosition> sectionPositions = null;
  public List<Section> children = null;

  public SectionPosition currentSectionPosition {
    get {
      return sectionPositions[sectionPositions.Count - 1];
    }
  }

  public Section(SectionPosition startingCurrentSectionPosition) {
    sectionPositions = new List<SectionPosition>();
    children = new List<Section>();
    sectionPositions.Add(startingCurrentSectionPosition);
  }

  public void AddChild(Section newChild) {
    children.Add(newChild);
  }
}
//-----------------------------------------
public class SectionParser {
  List<string> fileLines = null;
  Section rootSection = null;
  char startSectionDelimiterCharacter;
  char endSectionDelimiterCharacter;

  public SectionParser(List<string> newFileLines, char newStartSectionDelimiterCharacter, char newEndSectionDelimiterCharacter) {
    fileLines = newFileLines;

    startSectionDelimiterCharacter = newStartSectionDelimiterCharacter;
    endSectionDelimiterCharacter = newEndSectionDelimiterCharacter;
  }

  public void ReconfigureParserSettings(List<string> newFileLines, char newStartSectionDelimiterCharacter, char newEndSectionDelimiterCharacter) {
    fileLines = newFileLines;

    startSectionDelimiterCharacter = newStartSectionDelimiterCharacter;
    endSectionDelimiterCharacter = newEndSectionDelimiterCharacter;
  }

  public void ParseFileLines() {
    rootSection = null;
    Section currentSection = null;

    for(int i = 0; i < fileLines.Count; i++) {
      string currentLine = fileLines[i];
      // Debug.Log("Current Line:\n" + currentLine);
      for(int j = 0; j < currentLine.Length; j++) {
        char currentCharacter = currentLine[j];

        // start of a section
        if(currentCharacter.Equals(startSectionDelimiterCharacter)) {
          if(rootSection == null) {

            rootSection = new Section(new SectionPosition(i, j));
            currentSection = rootSection;
          }
          else {
            currentSection.currentSectionPosition.endLine = i;
            currentSection.currentSectionPosition.endCharIndex = j;

            Section nestedSection = new Section(new SectionPosition(i,j));
            nestedSection.parentSection = currentSection;

            currentSection.AddChild(nestedSection);
            currentSection = nestedSection;
          }
        }
        // end of a section
        if(currentCharacter.Equals(endSectionDelimiterCharacter)) {
          if(rootSection == null) {
            Debug.Log("CLOSING SECTION DELIMITER LOCATED WITHOUT ANY OPEN SECTION DELIMITER LOCATED. THIS IS AN ERROR, THE PASSED IN FILE IS IMPROPERLY FORMATTED.");
          }
          else {
            currentSection.currentSectionPosition.endLine = i;
            currentSection.currentSectionPosition.endCharIndex = j;

            if(currentSection.parentSection != null) {
              currentSection = currentSection.parentSection;
              currentSection.sectionPositions.Add(new SectionPosition(i, j));
            }
            else {
              // Debug.Log("A PARENT NODE OF NULL HAS BEEN ENCOUNTERED. THIS SHOULD BE THE ROOT OF THE TREE.");
            }
          }
        }
        else {
          // do nothing, unrecognized token
        }
      }
    }
  }

  public List<int[]> GetAllSectionStartAndEndLinesContainingString(string stringBeingSearchedFor) {
    return SearchSectionForString(rootSection, stringBeingSearchedFor);
  }

  public List<int[]> SearchSectionForString(Section sectionBeingSearched, string stringBeingSearchedFor) {
    List<int[]> locatedStartAndEndLines = new List<int[]>();

    int[] searchResults = DoesSectionContainString(sectionBeingSearched, stringBeingSearchedFor);
    if(searchResults != null) {
      locatedStartAndEndLines.Add(searchResults);
    }
    foreach(Section childSection in sectionBeingSearched.children) {
      List<int[]> returnedSearchResults = SearchSectionForString(childSection, stringBeingSearchedFor);
      locatedStartAndEndLines.AddRange(returnedSearchResults);
    }

    return locatedStartAndEndLines;
  }

  public int[] DoesSectionContainString(Section sectionBeingSearched, string stringBeingSearchedFor) {
    int smallestStartLineNumber = -1;
    int largestEndLineNumber = -1;
    bool foundStringBeingSearchedFor = false;

    foreach(SectionPosition sectionPositionBeingSearched in sectionBeingSearched.sectionPositions) {
      //-------------------
      // used to track the correct start and end sections for the entire section
      if(smallestStartLineNumber == -1
         || sectionPositionBeingSearched.startLine < smallestStartLineNumber) {
        smallestStartLineNumber = sectionPositionBeingSearched.startLine;
      }
      if(smallestStartLineNumber == -1
         || sectionPositionBeingSearched.endLine > largestEndLineNumber) {
        largestEndLineNumber = sectionPositionBeingSearched.endLine;
      }
      //-------------------

      int lineTracker = sectionPositionBeingSearched.startLine;
      int endLine = sectionPositionBeingSearched.endLine;

      while(lineTracker <= endLine) {
        string currentLine = fileLines[lineTracker];

        if(lineTracker == sectionPositionBeingSearched.startLine
           && lineTracker == sectionPositionBeingSearched.endLine) {
          int sectionTextLength = (sectionPositionBeingSearched.endCharIndex - sectionPositionBeingSearched.startCharIndex);
          if(currentLine.Substring(sectionPositionBeingSearched.startCharIndex, sectionTextLength).Contains(stringBeingSearchedFor)) {
            // Debug.Log("LOCATED STRING ON LINE: " + lineTracker);
            foundStringBeingSearchedFor = true;
          }
        }
        else if(lineTracker == sectionPositionBeingSearched.startLine) {
          int sectionTextLength = (currentLine.Length - sectionPositionBeingSearched.startCharIndex);
          if(currentLine.Substring(sectionPositionBeingSearched.startCharIndex, sectionTextLength).Contains(stringBeingSearchedFor)) {
            // Debug.Log("LOCATED STRING ON LINE: " + lineTracker);
            foundStringBeingSearchedFor = true;
          }
        }
        else if(lineTracker == endLine) {
          if(currentLine.Substring(0, sectionPositionBeingSearched.endCharIndex).Contains(stringBeingSearchedFor)) {
            // Debug.Log("LOCATED STRING ON LINE: " + lineTracker);
            foundStringBeingSearchedFor = true;
          }
        }
        else {
          if(currentLine.Contains(stringBeingSearchedFor)) {
            // Debug.Log("LOCATED STRING ON LINE: " + lineTracker);
            foundStringBeingSearchedFor = true;
          }
        }
        lineTracker++;
      }
    }

    if(foundStringBeingSearchedFor) {
      return new int[] { smallestStartLineNumber, largestEndLineNumber };
    }

    return null;
  }
}
//-----------------------------------------
/// <summary>
/// This is a script that was created for modifying the generated XCode project
/// created by Unity. This was done in order to add frameworks and build configurations
/// automatically so that the user will not have to worry about deploying the AdColony SDK on iOS.
///
/// This is done by parsing the pbxproj file that is create by unity when the
/// build step is performed. Once parsed it will then selectively inject new lines into
/// their respective locations within the pbxproj file so that upon opening in XCode
/// the frameworks will appear by default.
///
/// This script also copies over any specified frameworks (which in this case is
/// just the AdColony.framework file). It places it into a subdirectory of the
/// build folder that unity targets, and then configures the project build settings
/// so that the third-party framework folder is checked for frameworks.
/// </summary>
public static class AdColonyPostProcessBuild
{

  /// <summary>
  /// This is a class that is used to group all the information for adding a framework
  /// into the pbxproj file of the generated XCode project.
  /// </summary>
  public class Framework
  {
      /// <summary>This is the name of the framework.</summary>
      public string name;
      /// <summary>This is an arbitrary UUID. It CANNOT be the same as the fileId.</summary>
      public string id;
      /// <summary>This is an arbitrary UUID. It CANNOT be the same as the id.</summary>
      public string fileId;
      /// <summary>These will be attributes that are applied to the framework (Optional, Required, etc).</summary>
      public string[] attributes;
      /// <summary>This specifies the type of the framework being added.</summary>
      public string fileType;
      /// <summary>This is the path to the location of the framework.</summary>
      public string path;
      /// <summary>This is a flag to indicate if the framework has already been added to the pbxproj.</summary>
      public bool hasBeenAdded = false;
      /// <summary>This sets where the framework will be placed within the XCode project hierarchy.</summary>
      public string sourceTree;
      /// <summary>These are the groups that the framework will be added to.</summary>
      public List<string> pbxGroups;

      /// <summary>
      /// This constructor takes in all the values used within a framework and sets their respective properties.
      /// </summary>
      public Framework(string newName, string newId, string newFileId, string[] newAttributes, string newFileType, string newPath = "", string[] newPBXGroups = null, string newSourceTree = "SDKROOT") {
          name = newName;
          id = newId;
          fileId = newFileId;
          attributes = newAttributes;
          fileType = newFileType;
          path = newPath;
          sourceTree = newSourceTree;

          // These are the pbx group types as seen from a default unity build:
          // Products, CustomTemplate, Frameworks, Unity_iPhone_Tests, Supporting Files, UI, PluginBase, Unity, Classes, Libraries
          // Probably doesn't need to be a list...
          pbxGroups = new List<string>();
          foreach(string pbxGroup in newPBXGroups) {
            pbxGroups.Add(pbxGroup.Trim());
          }
      }
  }

  /// <summary>
  /// These are used as constant references for each specific section in the xcode project
  /// </summary>
  const string PBX_BUILD_FILE = "PBXBuildFile";
  const string PBX_FILE_REFERENCE = "PBXFileReference";
  const string PBX_CONTAINER_ITEM_PROXY = "PBXContainerItemProxy";
  const string PBX_COPY_FILES_BUILD_PHASE = "PBXCopyFilesBuildPhase";
  const string PBX_FRAMEWORKS_BUILD_PHASE = "PBXFrameworksBuildPhase";
  const string PBX_GROUP = "PBXGroup";
  const string PBX_NATIVE_TARGET = "PBXNativeTarget";
  const string PBX_PROJECT = "PBXProject";
  const string PBX_RESOURCES_BUILD_PHASE = "PBXResourcesBuildPhase";
  const string PBX_SHELL_SCRIPT_BUILD_PHASE = "PBXShellScriptBuildPhase";
  const string PBX_SOURCES_BUILD_PHASE = "PBXSourcesBuildPhase";
  const string PBX_TARGET_DEPENDENCY = "PBXTargetDependency";
  const string PBX_VARIANT_GROUP = "PBXVariantGroup";
  const string XC_BUILD_CONFIGURATION = "XCBuildConfiguration";
  const string XC_CONFIGURATION_LIST = "XCConfigurationList";

  /// <summary>
  /// This is the name of the folder that is created to place third party frameworks into when the xcode project is created
  /// </summary>
  const string THIRD_PARTY_FRAMEWORK_DIRECTORY_NAME = "Third-Party Frameworks";

  /// <summary>
  /// tabLength and tabSpace are used to specify how the indentation in the file should be configured
  /// </summary>
  static int tabLength = 2;
  static string tabSpace = new string(' ', tabLength);

  static SectionParser sectionParser = null;

  // Processbuild Function
  // For the sake of ease AdColony's Post Process Build script should ALWAYS be run last
  [PostProcessBuild(2000)] // <- this is where the magic happens, this attribute triggers unity to call this method when the build process has finished
  public static void OnPostProcessBuild(BuildTarget target, string path)
  {
      // Checks this is an iOS build before running
      #if UNITY_IPHONE
      {
        Debug.Log("ADCOLONY POST PROCESS BUILD HAS BEEN INITIATED.");

        // Declares all the frameworks that need to be injected
        Framework[] frameworksToAdd = {
          new Framework("AdColony.framework", "0277BC0B195E402F001C9760", "0277BC0A195E402F001C9760", new string[] {}, "wrapper.framework", "Third-Party Frameworks/AdColony.framework", new string[] {"Frameworks"}, "<group>"),
          new Framework("libz.1.2.5.dylib", "0277BBFD195E3FCA001C9760", "0277BBFC195E3FCA001C9760", new string[] {}, "\"compiled.mach-o.dylib\"", "usr/lib/libz.1.2.5.dylib", new string[] {"Frameworks"}, "SDKROOT"),
          new Framework("AdSupport.framework", "0277BBFF195E3FD4001C9760", "0277BBFE195E3FD4001C9760", new string[] {"Weak"}, "wrapper.framework", "System/Library/Frameworks/AdSupport.framework", new string[] {"Frameworks"}, "SDKROOT"),
          new Framework("CoreTelephony.framework", "0277BC01195E3FDD001C9760", "0277BC00195E3FDC001C9760", new string[] {}, "wrapper.framework", "System/Library/Frameworks/CoreTelephony.framework", new string[] {"Frameworks"}, "SDKROOT"),
          new Framework("EventKit.framework", "0277BC03195E3FE4001C9760", "0277BC02195E3FE4001C9760", new string[] {}, "wrapper.framework", "System/Library/Frameworks/EventKit.framework", new string[] {"Frameworks"}, "SDKROOT"),
          new Framework("MessageUI.framework", "0277BC05195E3FEB001C9760", "0277BC04195E3FEB001C9760", new string[] {}, "wrapper.framework", "System/Library/Frameworks/MessageUI.framework", new string[] {"Frameworks"}, "SDKROOT"),
          new Framework("Social.framework", "0277BC07195E3FF3001C9760", "0277BC06195E3FF3001C9760", new string[] {"Weak"}, "wrapper.framework", "System/Library/Frameworks/Social.framework", new string[] {"Frameworks"}, "SDKROOT"),
          new Framework("StoreKit.framework", "0277BC09195E3FFA001C9760", "0277BC08195E3FFA001C9760", new string[] {"Weak"}, "wrapper.framework", "System/Library/Frameworks/StoreKit.framework", new string[] {"Frameworks"}, "SDKROOT"),
          new Framework("AudioToolbox.framework", "8358D1B80ED1CC3700E3A684", "8358D1B70ED1CC3700E3A684", new string[] {}, "wrapper.framework", "System/Library/Frameworks/AudioToolbox.framework", new string[] {"Frameworks"}, "SDKROOT"),
          new Framework("AVFoundation.framework", "7F36C11313C5C673007FBDD9", "7F36C11013C5C673007FBDD9", new string[] {}, "wrapper.framework", "System/Library/Frameworks/AVFoundation.framework", new string[] {"Frameworks"}, "SDKROOT"),
          new Framework("CoreGraphics.framework", "56B7959B1442E0F20026B3DD", "56B7959A1442E0F20026B3DD", new string[] {}, "wrapper.framework","System/Library/Frameworks/CoreGraphics.framework", new string[] {"Frameworks"}, "SDKROOT"),
          new Framework("CoreMedia.framework", "7F36C11113C5C673007FBDD9", "7F36C10E13C5C673007FBDD9", new string[] {}, "wrapper.framework", "System/Library/Frameworks/CoreMedia.framework", new string[] {"Frameworks"}, "SDKROOT"),
          new Framework("EventKitUI.framework", "0277BC0D195E4068001C9760", "0277BC0C195E4068001C9760", new string[] {}, "wrapper.framework", "System/Library/Frameworks/EventKitUI.framework", new string[] {"Frameworks"}, "SDKROOT"),
          new Framework("MediaPlayer.framework", "5682F4B20F3B34FF007A219C", "5682F4B10F3B34FF007A219C", new string[] {}, "wrapper.framework", "System/Library/Frameworks/MediaPlayer.framework", new string[] {"Frameworks"}, "SDKROOT"),
          new Framework("QuartzCore.framework", "83B2570B0E62FF8A00468741", "83B2570A0E62FF8A00468741", new string[] {}, "wrapper.framework", "System/Library/Frameworks/QuartzCore.framework", new string[] {"Frameworks"}, "SDKROOT"),
          new Framework("SystemConfiguration.framework", "56BCBA390FCF049A0030C3B2", "56BCBA380FCF049A0030C3B2", new string[] {}, "wrapper.framework", "System/Library/Frameworks/SystemConfiguration.framework", new string[] {"Frameworks"}, "SDKROOT")
        };

        string xcodeDirectoryPath = UnityEditor.EditorUserBuildSettings.GetBuildLocation(BuildTarget.iPhone);
        string xcodeCustomThirdPartyDirectoryPath = Path.Combine(xcodeDirectoryPath, THIRD_PARTY_FRAMEWORK_DIRECTORY_NAME);
        string xcodeProjectPath = Path.Combine(xcodeDirectoryPath, "Unity-iPhone.xcodeproj");
        string xcodePBXProjectPath = Path.Combine(xcodeProjectPath, "project.pbxproj");
        string unityAssetDirectory = Application.dataPath;

        // Before injecting anything into the pbxproj file copy over the third party frameworks first
        CopyOverThirdPartyFrameworks(unityAssetDirectory, xcodeCustomThirdPartyDirectoryPath);

        // Now start updating the XCode pbxproj file with the frameworks
        updateXcodeProject(xcodePBXProjectPath, xcodeCustomThirdPartyDirectoryPath, frameworksToAdd) ;
      }
     #endif
     Debug.Log("ADCOLONY POST PROCESS BUILD HAS FINISHED") ;
  }

  /// <summary>
  /// This is the main method where the pbxproj file will be modified. In the body of the method there will be calls to other methods which will perform their logic for inserting the respective configurations into the pbxproj file.
  /// This finishes by writing out the entire new pbxproj file
  /// </summary>
  /// <param name="pbxProjectFilePath">This is the path to the pbxproj file being modified</param>
  /// <param name="thirdPartyFrameworkDirectoryPath">This is the path to the third-party frameworks directory being added into the XCode project's root directory.</param>
  /// <param name="frameworksToAdd">This is an array of frameworks that will be added to the pbxproj file.</param>
  public static void updateXcodeProject(string pbxProjectFilePath, string thirdPartyFrameworkDirectoryPath, Framework[] frameworksToAdd)
  {
    // STEP 1: Open the pbx project file and read in the lines to a list so it can have new lines injected
    string[] lines = System.IO.File.ReadAllLines(pbxProjectFilePath);
    List<string> linesList = new List<string>();
    foreach(string line in lines) {
      linesList.Add(line);
    }
    sectionParser = new SectionParser(linesList, '{', '}');

    // STEP 2: Next check which frameworks have already been added. If they have been added don't bother adding them
    //         This is probably redundant, but it felt like it was something worthwhile to do
    MarkFrameworksAlreadyAdded(lines, frameworksToAdd);
    // Debug.Log("Frameworks being added to the XCode Project:");
    foreach(Framework framework in frameworksToAdd) {
      if(!framework.hasBeenAdded) {
        // Debug.Log(framework.name + " will be added to the XCode project.");
      }
    }

    // STEP 3: Next each section in the pbxproj file will be modified to include the passed in list of
    Debug.Log("AdColony is adding frameworks to the PBXBuildFile section.");
    AddFrameworksToPBXBuildFileSection(linesList, frameworksToAdd);
    // Debug.Log("-------------------------------------------------");
    Debug.Log("AdColony is adding frameworks to the PBXBuildFileReference section.");
    AddFrameworksToPBXFileReferenceSection(linesList, frameworksToAdd);
    // Debug.Log("-------------------------------------------------");
    Debug.Log("AdColony is adding frameworks to the PBXFrameworksBuildPhase section.");
    AddFrameworksToPBXFrameworksBuildPhaseSection(linesList, frameworksToAdd);
    // Debug.Log("-------------------------------------------------");
    Debug.Log("AdColony is adding frameworks to the PBXGroup section.");
    AddFrameworksToPBXGroupSection(linesList, frameworksToAdd);
    // Debug.Log("-------------------------------------------------");
    // Debug.Log("AdColony is adding build settings to targets.");
    AddConfigurationsToXCBuildConfiguration(linesList, thirdPartyFrameworkDirectoryPath, frameworksToAdd);

    // STEP 4: Output the finally configured pbxproj file lines to the new file
    FileStream filestr = new FileStream(pbxProjectFilePath, FileMode.Create); //Create new file and open it for read and write, if the file exists overwrite it.
    filestr.Close() ;
    StreamWriter fCurrentXcodeProjFile = new StreamWriter(pbxProjectFilePath) ; // will be used for writing
    foreach(string line in linesList) {
      fCurrentXcodeProjFile.WriteLine(line);
    }
    fCurrentXcodeProjFile.Close();
  }
  //-----------------------------------------
  /// <summary>
  /// This method will parse the lines passed in from the pbxprojFileLines and compare them with the array of frameworksToAdd. Any matches found will then mark the framework as already added so that redundancy will not occur
  /// </summary>
  /// <param name="pbxprojFileLines">This is the array of lines that are in a pbxproj file.</param>
  /// <param name="frameworksToAdd">This is an array of frameworks that will be added to the pbxproj file.</param>
  private static void MarkFrameworksAlreadyAdded(string[] pbxprojFileLines, Framework[] frameworksToAdd) {
    foreach(string pbxprojFileLine in pbxprojFileLines) {
      foreach(Framework framework in frameworksToAdd) {
        if(pbxprojFileLine.Contains(framework.name)) {
          framework.hasBeenAdded = true;
        }
      }
    }
  }

  //----------------------------------------------------------------------------
  // LOGIC FOR HANDLING INJECTION GOES HERE
  //----------------------------------------------------------------------------
  /// <summary>
  /// This method will locate the PBXBuildFile section and will add the frameworks to the list of files
  /// </summary>
  /// <param name="pbxprojFileLines">This is the list of lines that are in a pbxproj file and will be modified to inject the new project lines.</param>
  /// <param name="frameworksToAdd">This is an array of frameworks that will be added to the pbxproj file.</param>
  private static void AddFrameworksToPBXBuildFileSection(List<string> pbxprojFileLines, Framework[] frameworksToAdd) {
    string stringToSearchFor = "isa = " + PBX_BUILD_FILE;
    List<int[]> sectionStartAndEndLines = null;
    sectionParser.ReconfigureParserSettings(pbxprojFileLines, '{', '}');
    sectionParser.ParseFileLines();
    sectionStartAndEndLines = sectionParser.GetAllSectionStartAndEndLinesContainingString(stringToSearchFor);

    int startLine = sectionStartAndEndLines[0][0];
    pbxprojFileLines.Insert(startLine, tabSpace + tabSpace + "/* END OF ADCOLONY INJECTED FILES */");
    foreach(Framework framework in frameworksToAdd) {
      string buildFileStringToInject = CreatePBXBuildFileStringToInject(framework);
      pbxprojFileLines.Insert(startLine, buildFileStringToInject);
    }
    pbxprojFileLines.Insert(startLine, tabSpace + tabSpace + "/* START OF ADCOLONY INJECTED FILES */");
  }
  //-----------------------------------------
  /// <summary>
  /// This method will locate the PBXFileReference section and will add the frameworks to the list of files
  /// </summary>
  /// <param name="pbxprojFileLines">This is the list of lines that are in a pbxproj file and will be modified to inject the new project lines.</param>
  /// <param name="frameworksToAdd">This is an array of frameworks that will be added to the pbxproj file.</param>
  private static void AddFrameworksToPBXFileReferenceSection(List<string> pbxprojFileLines, Framework[] frameworksToAdd) {
    string stringToSearchFor = "isa = " + PBX_FILE_REFERENCE;
    List<int[]> sectionStartAndEndLines = null;
    sectionParser.ReconfigureParserSettings(pbxprojFileLines, '{', '}');
    sectionParser.ParseFileLines();
    sectionStartAndEndLines = sectionParser.GetAllSectionStartAndEndLinesContainingString(stringToSearchFor);

    int startLine = sectionStartAndEndLines[0][0];
    pbxprojFileLines.Insert(startLine, tabSpace + tabSpace + "/* END OF ADCOLONY INJECTED FILES */");
    foreach(Framework framework in frameworksToAdd) {
      string buildFileReferenceStringToInject = CreatePBXBuildFileReferenceStringToInject(framework);
      pbxprojFileLines.Insert(startLine, buildFileReferenceStringToInject);
    }
    pbxprojFileLines.Insert(startLine, tabSpace + tabSpace + "/* START OF ADCOLONY INJECTED FILES */");
  }
  //-----------------------------------------
  /// <summary>
  /// This method will locate the PBXFrameworksBuildPhase section and will add the frameworks to the list of files
  /// </summary>
  /// <param name="pbxprojFileLines">This is the list of lines that are in a pbxproj file and will be modified to inject the new project lines.</param>
  /// <param name="frameworksToAdd">This is an array of frameworks that will be added to the pbxproj file.</param>
  private static void AddFrameworksToPBXFrameworksBuildPhaseSection(List<string> pbxprojFileLines, Framework[] frameworksToAdd) {
    string stringToSearchFor = "isa = " + PBX_FRAMEWORKS_BUILD_PHASE;
    List<int[]> sectionStartAndEndLines = null;
    sectionParser.ReconfigureParserSettings(pbxprojFileLines, '{', '}');
    sectionParser.ParseFileLines();
    sectionStartAndEndLines = sectionParser.GetAllSectionStartAndEndLinesContainingString(stringToSearchFor);

    foreach(int[] sectionStartAndEndLineNumbers in sectionStartAndEndLines) {
      List<int[]> fileSubSectionStartAndEndLines = LocateSectionStartAndEndLines(pbxprojFileLines, sectionStartAndEndLineNumbers[0], sectionStartAndEndLineNumbers[1], @"^\s*(files = \()\s*$", @"^\s*(\);)\s*$");

      int multipleSubSectionOffset = 0;
      foreach(int[] fileSubSectionStartAndEndLine in fileSubSectionStartAndEndLines) {
        pbxprojFileLines.Insert((fileSubSectionStartAndEndLine[0] + multipleSubSectionOffset), tabSpace + tabSpace + tabSpace + tabSpace + "/* START OF ADCOLONY INJECTED FILES */");
        multipleSubSectionOffset++;
        foreach(Framework framework in frameworksToAdd) {
          string buildFrameworksBuildPhaseFileStringToInject = CreateFileSubsectionStringToInject(framework);
          pbxprojFileLines.Insert((fileSubSectionStartAndEndLine[0] + multipleSubSectionOffset), buildFrameworksBuildPhaseFileStringToInject);
          multipleSubSectionOffset++;
        }
        pbxprojFileLines.Insert((fileSubSectionStartAndEndLine[0] + multipleSubSectionOffset), tabSpace + tabSpace + tabSpace + tabSpace + "/* END OF ADCOLONY INJECTED FILES */");
        multipleSubSectionOffset++;
      }
    }
  }
  //----------------------------------------------------------------------------
  /// <summary>
  /// This method will locate the PBXGroup section and will add the frameworks to the list of files
  /// </summary>
  /// <param name="pbxprojFileLines">This is the list of lines that are in a pbxproj file and will be modified to inject the new project lines.</param>
  /// <param name="frameworksToAdd">This is an array of frameworks that will be added to the pbxproj file.</param>
  private static void AddFrameworksToPBXGroupSection(List<string> pbxprojFileLines, Framework[] frameworksToAdd) {
    string stringToSearchFor = "isa = " + PBX_GROUP;
    List<int[]> sectionStartAndEndLines = null;
    sectionParser.ReconfigureParserSettings(pbxprojFileLines, '{', '}');
    sectionParser.ParseFileLines();
    sectionStartAndEndLines = sectionParser.GetAllSectionStartAndEndLinesContainingString(stringToSearchFor);

    foreach(int[] sectionStartAndEndLineNumbers in sectionStartAndEndLines) {
      bool isFrameworksPBXGroup = pbxprojFileLines[sectionStartAndEndLineNumbers[0]].Contains("Frameworks");
      if(isFrameworksPBXGroup) {
        List<int[]> fileSubSectionStartAndEndLines = LocateSectionStartAndEndLines(pbxprojFileLines, sectionStartAndEndLineNumbers[0], sectionStartAndEndLineNumbers[1], @"^\s*(children = \()\s*$", @"^\s*(\);)\s*$");
        foreach(int[] fileSubSectionStartAndEndLine in fileSubSectionStartAndEndLines) {
          int multipleSubSectionOffset = 0;

          pbxprojFileLines.Insert((fileSubSectionStartAndEndLine[0] + multipleSubSectionOffset), tabSpace + tabSpace + tabSpace + tabSpace + "/* START OF ADCOLONY INJECTED FILES */");
          multipleSubSectionOffset++;
          foreach(Framework framework in frameworksToAdd) {
            string buildPBXGroupChildrenStringToInject = CreatePBXGroupChildrenSubsectionStringToInject(framework);
            pbxprojFileLines.Insert((fileSubSectionStartAndEndLine[0] + multipleSubSectionOffset), buildPBXGroupChildrenStringToInject);
            multipleSubSectionOffset++;
          }
          pbxprojFileLines.Insert((fileSubSectionStartAndEndLine[0] + multipleSubSectionOffset), tabSpace + tabSpace + tabSpace + tabSpace + "/* END OF ADCOLONY INJECTED FILES */");
          multipleSubSectionOffset++;
        }
      }
      else {
        // Debug.Log("THE PBXGROUP 'Frameworks' HAS NOT BEEN LOCATED! FRAMEWORKS WILL NOT BE ADDED TO THE PBXGROUP!");
      }
    }
  }

  /// <summary>
  /// This method will locate the XCBuildConfiguration section and will modify each section so that the build process will be correctly configured for
  /// </summary>
  /// <param name="pbxprojFileLines">This is the list of lines that are in a pbxproj file and will be modified to inject the new project lines.</param>
  /// <param name="thirdPartyFrameworkDirectoryPath">This is a string that will point to the third party frameworks directory that was created.</param>
  /// <param name="frameworksToAdd">This is an array of frameworks that will be added to the pbxproj file.</param>
  private static void AddConfigurationsToXCBuildConfiguration(List<string> pbxprojFileLines, string thirdPartyFrameworkDirectoryPath, Framework[] frameworksToAdd) {
    int addedLinesOffset = 0;

    // These are the build settings that need to be added to the configuration section
    string thirdPartyFrameworkPathToAdd = tabSpace + tabSpace + tabSpace + tabSpace + tabSpace + "\"\\\"$(SRCROOT)/" + THIRD_PARTY_FRAMEWORK_DIRECTORY_NAME + "\\\"\",";
    string objCFlagToAdd = tabSpace + tabSpace + tabSpace + tabSpace + tabSpace + "-ObjC,";

    // Starts the dive into XCBuildConfiguration section
    // List<int[]> sectionStartAndEndLines = LocateSectionStartAndEndLines(pbxprojFileLines, 0, pbxprojFileLines.Count, GetBeginSectionRegex(XC_BUILD_CONFIGURATION), GetEndSectionRegex(XC_BUILD_CONFIGURATION));
    string stringToSearchFor = "isa = " + XC_BUILD_CONFIGURATION;
    List<int[]> sectionStartAndEndLines = null;
    sectionParser.ReconfigureParserSettings(pbxprojFileLines, '{', '}');
    sectionParser.ParseFileLines();
    sectionStartAndEndLines = sectionParser.GetAllSectionStartAndEndLinesContainingString(stringToSearchFor);
    foreach(int[] sectionStartAndEndLineNumbers in sectionStartAndEndLines) {
      //------------------------------------------------------------------------
      // Starts the dive into XCBuildConfiguration - Build Settings section
      List<int[]> buildSettingsSectionStartAndEndLines = LocateSectionStartAndEndLines(pbxprojFileLines, (sectionStartAndEndLineNumbers[0] + addedLinesOffset), (sectionStartAndEndLineNumbers[1] + addedLinesOffset), @"^\s*buildSettings = {\s*$", @"^\s*(\};)\s*$");
      foreach(int[] buildSettingsStartAndEndLineNumbers in buildSettingsSectionStartAndEndLines) {
        //----------------------------------------------------------------------
        // Starts the dive into Search - FRAMEWORK_SEARCH_PATHS section
        List<int[]> frameworkSearchPathSectionStartAndEndLines = LocateSectionStartAndEndLines(pbxprojFileLines, buildSettingsStartAndEndLineNumbers[0], buildSettingsStartAndEndLineNumbers[1], @"^\s*FRAMEWORK_SEARCH_PATHS = \(\s*$", @"^\s*\);\s*$");
        // If no FRAMEWORK_SEARCH_PATHS section was NOT found
        if(frameworkSearchPathSectionStartAndEndLines.Count == 0) {
          pbxprojFileLines.Insert((buildSettingsStartAndEndLineNumbers[0] + 0), tabSpace + tabSpace + tabSpace + tabSpace + "/* START OF ADCOLONY INJECTED FILES */");
          addedLinesOffset++;

          pbxprojFileLines.Insert((buildSettingsStartAndEndLineNumbers[0] + 1), tabSpace + tabSpace + tabSpace + tabSpace + "FRAMEWORK_SEARCH_PATHS = (");
          addedLinesOffset++;

          pbxprojFileLines.Insert((buildSettingsStartAndEndLineNumbers[0] + 2), thirdPartyFrameworkPathToAdd);
          addedLinesOffset++;

          pbxprojFileLines.Insert((buildSettingsStartAndEndLineNumbers[0] + 3), tabSpace + tabSpace + tabSpace + tabSpace + ");");
          addedLinesOffset++;

          pbxprojFileLines.Insert((buildSettingsStartAndEndLineNumbers[0] + 4), tabSpace + tabSpace + tabSpace + tabSpace + "/* END OF ADCOLONY INJECTED FILES */");
          addedLinesOffset++;
        }
        // If a FRAMEWORK_SEARCH_PATHS has was found
        else {
          foreach(int[] frameworkSearchPathSectionStartAndEndLineNumbers in frameworkSearchPathSectionStartAndEndLines) {
            pbxprojFileLines.Insert(frameworkSearchPathSectionStartAndEndLineNumbers[0], tabSpace + tabSpace + tabSpace + tabSpace + tabSpace + "/* START OF ADCOLONY INJECTED FILES */");
            addedLinesOffset++;

            pbxprojFileLines.Insert(frameworkSearchPathSectionStartAndEndLineNumbers[0] + 1, thirdPartyFrameworkPathToAdd);
            addedLinesOffset++;

            pbxprojFileLines.Insert(frameworkSearchPathSectionStartAndEndLineNumbers[0] + 2, tabSpace + tabSpace + tabSpace + tabSpace + tabSpace + "/* END OF ADCOLONY INJECTED FILES */");
            addedLinesOffset++;
          }
        }
        //----------------------------------------------------------------------
        // Starts the dive into Search - OTHER_LDFLAGS section
        List<int[]> otherLDFlagsSectionStartAndEndLines = LocateSectionStartAndEndLines(pbxprojFileLines, buildSettingsStartAndEndLineNumbers[0], buildSettingsStartAndEndLineNumbers[1], @"^\s*OTHER_LDFLAGS = \(\s*$", @"^\s*\);\s*$");
        // If no OTHER_LDFLAGS section was NOT found
        if(otherLDFlagsSectionStartAndEndLines.Count == 0) {
          pbxprojFileLines.Insert((buildSettingsStartAndEndLineNumbers[0] + 0), tabSpace + tabSpace + tabSpace + tabSpace + "/* START OF ADCOLONY INJECTED FILES */");
          addedLinesOffset++;

          pbxprojFileLines.Insert((buildSettingsStartAndEndLineNumbers[0] + 1), tabSpace + tabSpace + tabSpace + tabSpace + "OTHER_LDFLAGS = (");
          addedLinesOffset++;

          pbxprojFileLines.Insert((buildSettingsStartAndEndLineNumbers[0] + 2), objCFlagToAdd);
          addedLinesOffset++;

          pbxprojFileLines.Insert((buildSettingsStartAndEndLineNumbers[0] + 3), tabSpace + tabSpace + tabSpace + tabSpace + ");");
          addedLinesOffset++;

          pbxprojFileLines.Insert((buildSettingsStartAndEndLineNumbers[0] + 4), tabSpace + tabSpace + tabSpace + tabSpace + "/* END OF ADCOLONY INJECTED FILES */");
          addedLinesOffset++;
        }
        // If a OTHER_LDFLAGS has was found
        else {
          foreach(int[] otherLDFlagsSectionStartAndEndLineNumbers in otherLDFlagsSectionStartAndEndLines) {
            pbxprojFileLines.Insert(otherLDFlagsSectionStartAndEndLineNumbers[0], tabSpace + tabSpace + tabSpace + tabSpace + tabSpace + "/* START OF ADCOLONY INJECTED FILES */");
            addedLinesOffset++;

            pbxprojFileLines.Insert(otherLDFlagsSectionStartAndEndLineNumbers[0] + 1, objCFlagToAdd);
            addedLinesOffset++;

            pbxprojFileLines.Insert(otherLDFlagsSectionStartAndEndLineNumbers[0] + 2, tabSpace + tabSpace + tabSpace + tabSpace + tabSpace + "/* END OF ADCOLONY INJECTED FILES */");
            addedLinesOffset++;
          }
        }
      }
      //------------------------------------------------------------------------
    }
  }
  //----------------------------------------------------------------------------
  // LINE PARSING CODE GOES HERE
  //----------------------------------------------------------------------------
  /// <summary>
  /// This method will return the regex to locate the beginning of a section inside of a PBXProj file.
  /// </summary>
  /// <param name="sectionName">This the name of the section you would like to be located. It's best to use the constants declared at the start of this code file to pass into this, in order to guarantee no issues.</param>
  /// <returns>This returns the regex used to located the beginning of a section in a XCode pbxproj file.</returns>
  private static string GetBeginSectionRegex(string sectionName) {
    string beginRegex = "";
    beginRegex += @"^\s*\/\*\s*Begin*\s(" + sectionName + @")\s*section\s*\*\/$";
    return beginRegex;
  }

  /// <summary>
  /// This method will return the regex to locate the ending of a section inside of a PBXProj file.
  /// </summary>
  /// <param name="sectionName">This the name of the section you would like to be located. It's best to use the constants declared at the start of this code file to pass into this, in order to guarantee no issues.</param>
  /// <returns>This returns the regex used to located the ending of a section in a XCode pbxproj file.</returns>
  private static string GetEndSectionRegex(string sectionName) {
    string endRegex = "";
    endRegex += @"^\s*\/\*\s*End*\s(" + sectionName + @")\s*section\s*\*\/$";
    return endRegex;
  }
  //-----------------------------------------
  /// <summary>
  /// This method will search between specified starting and end lines for two lines that will indicate the start and end of a section. The start and stop sections will be located by the corresponding regex strings passed in.
  /// This will locate multiple section as  long as the sections are siblings. This means each section will need to be started and end before another section can be located. It will not locate a section as a child of another section.
  /// </summary>
  /// <param name="pbxprojFileLines">This is the list of lines that are in a pbxproj file and will be modified to inject the new project lines.</param>
  /// <param name="sectionStartLineNumber">This is the start line where searching will start to be performed. All lines before this will be ignored.</param>
  /// <param name="sectionEndLineNumber">This is the end line where searching will end. All lines after this will be ignored.</param>
  /// <param name="startSectionRegexString">This is the regex used to identify the starting line in the file.</param>
  /// <param name="endSectionRegexString">This is the regex used to identify the end line in the file.</param>
  /// <returns>This returns a list of integer arrays. This is because Unity's .NET version doesn not support Tuples(Supported in .NET 4). The integer arrays will be a length of two with the first index representing the start line, and the second index representing the end line of the section.</returns>
  private static List<int[]> LocateSectionStartAndEndLines(List<string> pbxprojFileLines, int sectionStartLineNumber, int sectionEndLineNumber, string startSectionRegexString, string endSectionRegexString) {

    List<int[]> fileSectionStartAndEndLineNumbers = new List<int[]>();

    // This regex identifies which section has been encountered and pulls out a reference to the section
    Regex beginOfFilesSectionRegex = new Regex(startSectionRegexString, RegexOptions.IgnoreCase);
    Regex endOfFilesSectionRegex = new Regex(endSectionRegexString, RegexOptions.IgnoreCase);

    int foundSectionStartLineNumber = -1;
    int foundSectionEndLineNumber = -1;

    int lineTracker = sectionStartLineNumber;
    while(lineTracker < sectionEndLineNumber) {
      Match beginMatch = beginOfFilesSectionRegex.Match(pbxprojFileLines[lineTracker]);
      Match endMatch = endOfFilesSectionRegex.Match(pbxprojFileLines[lineTracker]);
      if(beginMatch.Success) {
        foundSectionStartLineNumber = lineTracker;
      }
      if(endMatch.Success) {
        foundSectionEndLineNumber = lineTracker;
      }

      if(foundSectionStartLineNumber == -1
         && foundSectionEndLineNumber > -1) {
        foundSectionEndLineNumber = -1;
      }

      // Adds lines to list if found, then resets to find next section
      if(foundSectionStartLineNumber != -1
         && foundSectionEndLineNumber != -1) {
        // The values are incremented by 1 in order to offset correctly for insertion into the list
        foundSectionStartLineNumber++;
        foundSectionEndLineNumber++;

        fileSectionStartAndEndLineNumbers.Add( new int[] { foundSectionStartLineNumber, foundSectionEndLineNumber } );

        foundSectionStartLineNumber = -1;
        foundSectionEndLineNumber = -1;
      }
      lineTracker++;
    }

    return fileSectionStartAndEndLineNumbers;
  }

  //----------------------------------------------------------------------------
  // STRING BUILDING FUNCTIONS FOR INJECTION GO HERE
  //----------------------------------------------------------------------------
  /// <summary>
  /// This method takes in a framework and using the framework's personal properties will create a string that in the pbxproj format to inject it into the PBXBuildFile section.
  /// </summary>
  /// <param name="framework">This is the framework that will be injected into the pbxproj file. Its properties will be used to format the string that is created.</param>
  /// <returns>This returns a string formatted like a PBXBuildFile string.</returns>
  private static string CreatePBXBuildFileStringToInject(Framework framework) {
    string subsection = "Frameworks" ;
    string stringToInject = "";

    stringToInject += tabSpace + tabSpace;
    stringToInject += framework.id;
    stringToInject += " /* " + framework.name + " in " + subsection + " */";

    // start of build file collection with bracket, then adds info, then performs condition modifications, then closing collection
    stringToInject += " = {";
    stringToInject += "isa = PBXBuildFile; fileRef = " + framework.fileId + " /* " + framework.name + " */;";
    stringToInject += " settings = {";

    if(framework.attributes != null
       && framework.attributes.Length > 0) {
      stringToInject += "ATTRIBUTES = (";
      foreach(string attribute in framework.attributes) {
        stringToInject += attribute + ", ";
      }
      stringToInject += ");";
    }
    stringToInject += " }; };";
    // stringToInject += "\n";

    return stringToInject;
  }
  //-----------------------------------------
  /// <summary>
  /// This method takes in a framework and using the framework's personal properties will create a string that is in the same format to inject it into the PBXBuildFileReference section.
  /// </summary>
  /// <param name="framework">This is the framework that will be injected into the pbxproj file. Its properties will be used to format the string that is created.</param>
  /// <returns>This returns a string formatted like a PBXBuildFileReference string.</returns>
  private static string CreatePBXBuildFileReferenceStringToInject(Framework framework) {
    string stringToInject = "";

    stringToInject += tabSpace + tabSpace;
    stringToInject += framework.fileId + " /* " + framework.name + " */ = {isa = PBXFileReference; lastKnownFileType = " + framework.fileType + "; name = " + framework.name + "; path = \"" + framework.path + "\"; sourceTree = \"" + framework.sourceTree + "\"; };";
    // stringToInject += "\n";

    return stringToInject;
  }
  //-----------------------------------------
  /// <summary>
  /// This method takes in a framework and using the framework's personal properties will create a string that is in the same format to inject it into the 'files' subsection of a section.
  /// </summary>
  /// <param name="framework">This is the framework that will be injected into the pbxproj file. Its properties will be used to format the string that is created.</param>
  /// <returns>This returns a string formatted like file subsection's string.</returns>
  private static string CreateFileSubsectionStringToInject(Framework framework) {
    string stringToInject = "";

    stringToInject += tabSpace + tabSpace + tabSpace + tabSpace;
    stringToInject += framework.id + " /* " + framework.name + " in Frameworks */,";
    // stringToInject += "\n";

    return stringToInject;
  }
  //-----------------------------------------
  /// <summary>
  /// This method takes in a framework and using the framework's personal properties will create a string that is in the same format to inject it into the 'children' subsection of a section.
  /// </summary>
  /// <param name="framework">This is the framework that will be injected into the pbxproj file. Its properties will be used to format the string that is created.</param>
  /// <returns>This returns a string formatted like file subsection's string.</returns>
  private static string CreatePBXGroupChildrenSubsectionStringToInject(Framework framework) {
      string stringToInject = "";
      stringToInject += tabSpace + tabSpace + tabSpace + tabSpace + framework.fileId + " /* " + framework.name + " */,";
      // stringToInject += "\n";
      return stringToInject;
  }

  //----------------------------------------------------------------------------
  // FUNCTIONS FOR COPYING THIRD PARTY FRAMEWORKS OVER
  //----------------------------------------------------------------------------
  /// <summary> This takes in the path to the root directory being searched for frameworks, and then generates a list of their absolute paths. Once it locates each framework it will copy each one of them over. It should be noted that frameworks in iOS are actually directories, so this is more of a directory copy.</summary>
  /// <param name="projectRootDirectory">This is the Assets folder for the unity project being built</param>
  /// <param name="targetDirectory">This is the target directory that the frameworks should be copied to.</param>
  private static void CopyOverThirdPartyFrameworks(string projectRootDirectory, string targetDirectory) {
    // string array of the frameworks to copy over
    string[] thirdPartyFrameworksToCopy = { "AdColony.framework" };

    if(Directory.Exists(projectRootDirectory)) {
      // get root directory folders
      Dictionary<string, string> directoryResults = new Dictionary<string, string>();

      // for each directory in the root directory
      foreach(string directoryBeingSought in thirdPartyFrameworksToCopy) {
        SearchDirectoryForDirectory(projectRootDirectory, directoryBeingSought, true, directoryResults);
      }

      if(!Directory.Exists(targetDirectory)) {
        Debug.Log("CREATING A THIRD PARTY FRAMEWORKS FOLDER AT:\n" + targetDirectory);
        Directory.CreateDirectory(targetDirectory);
      }
      foreach(KeyValuePair<string, string> dir in directoryResults) {
        // Debug.Log("Key: " + dir.Key + " Value: " + dir.Value);
        CopyDirectoryIntoDirectory(dir.Value, targetDirectory, true);
      }
    }
  }

  /// <summary> This takes in the path to the root directory being searched for frameworks, and then generates a list of their absolute paths. Once it locates each framework it will move them over. It should be noted that frameworks in iOS are actually directories, so this is more of a directory copy.</summary>
  /// <param name="projectRootDirectory">This is the Assets folder for the unity project being built</param>
  /// <param name="targetDirectory">This is the target directory that the frameworks should be copied to.</param>
  private static void SearchDirectoryForDirectory(string directoryToSearch, string directoryNameBeingSearchedFor, bool searchRecursively, Dictionary<string, string> directoryResults) {
    foreach(string directory in Directory.GetDirectories(directoryToSearch)) {
      if(Directory.Exists(directory)) {
        // Debug.Log(dir);
        string dir = directory.Substring(directory.LastIndexOf(Path.DirectorySeparatorChar) + 1);

        if(dir.Equals(directoryNameBeingSearchedFor)) {
          if(!directoryResults.ContainsKey(dir)) {
            directoryResults[dir] = directory;
          }
        }

        if(searchRecursively) {
          SearchDirectoryForDirectory(directory, directoryNameBeingSearchedFor, searchRecursively, directoryResults);
        }
      }
    }
  }

  /// <summary> This will copy a directory into another directory..</summary>
  /// <param name="directoryToCopy">This is the path of the directory to copy.</param>
  /// <param name="targetDirectory">This is the path where the directory should be copied to.</param>
  /// <param name="copyRecursively">If true it will copy sub-directories of the initial directory specified as well.</param>
  private static void CopyDirectoryIntoDirectory(string directoryToCopy, string directoryToCopyTo, bool copyRecursively) {
    string directoryToCopyName = directoryToCopy.Substring(directoryToCopy.LastIndexOf(Path.DirectorySeparatorChar) + 1);
    string directoryToCopyToWithNewDirectory = Path.Combine(directoryToCopyTo, directoryToCopyName);

    if(!Directory.Exists(directoryToCopyToWithNewDirectory)) {
      Directory.CreateDirectory(directoryToCopyToWithNewDirectory);
    }

    string[] currentDirectorySubdirectoryInfo = Directory.GetDirectories(directoryToCopy);
    string[] currentDirectoryFiles = Directory.GetFiles(directoryToCopy);

    foreach(string dirFile in currentDirectoryFiles) {
      string fileName = dirFile.Substring(dirFile.LastIndexOf(Path.DirectorySeparatorChar) + 1);
      string fullPathToNewFile = Path.Combine(directoryToCopyToWithNewDirectory, fileName);
      Debug.Log("Copying: " + dirFile + "\nTo: " + fullPathToNewFile);

      File.Copy(dirFile, fullPathToNewFile, true);
    }

    if(copyRecursively) {
      foreach(string subdir in currentDirectorySubdirectoryInfo) {
        CopyDirectoryIntoDirectory(subdir, directoryToCopyToWithNewDirectory, copyRecursively);
      }
    }
  }
}
