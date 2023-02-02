
  // This file was automatically generated. Edits will be overwritten

  export interface Typegen0 {
        '@@xstate/typegen': true;
        internalEvents: {
          "": { type: "" };
"done.invoke.readUserData": { type: "done.invoke.readUserData"; data: unknown; __tip: "See the XState TS docs to learn how to strongly type this." };
"error.platform.readUserData": { type: "error.platform.readUserData"; data: unknown };
"xstate.init": { type: "xstate.init" };
"xstate.stop": { type: "xstate.stop" };
        };
        invokeSrcNameMap: {
          "readUserData": "done.invoke.readUserData";
        };
        missingImplementations: {
          actions: never;
          delays: never;
          guards: never;
          services: never;
        };
        eventsCausingActions: {
          "logout": "LOGOUT" | "xstate.stop";
"redirectToGoogleLogin": "";
"saveUser": "done.invoke.readUserData";
        };
        eventsCausingDelays: {
          
        };
        eventsCausingGuards: {
          "canLogin": "AUTHENTICATE";
"canRestoreData": "";
"isLoggedIn": "";
        };
        eventsCausingServices: {
          "readUserData": "";
        };
        matchesStates: "Loading Data" | "Logged In" | "Logging In" | "Not Logged In" | "Redirect To Google Login" | "Restore From localStorage";
        tags: never;
      }
  