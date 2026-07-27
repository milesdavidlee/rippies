#import <React/RCTBridgeModule.h>
#import <React/RCTEventEmitter.h>
#import <UIKit/UIKit.h>
#import <objc/message.h>

@interface RippiesUnityReveal : RCTEventEmitter <RCTBridgeModule>

@property(nonatomic, strong) id unityFramework;
@property(nonatomic, weak) UIWindow *reactWindow;
@property(nonatomic, copy) NSString *pendingRevealPayload;
@property(nonatomic, copy) NSString *pendingRevealId;
@property(nonatomic, assign) BOOL unitySceneLoaded;
@property(nonatomic, assign) BOOL returningToReact;

@end

@implementation RippiesUnityReveal

RCT_EXPORT_MODULE();

+ (BOOL)requiresMainQueueSetup
{
  return YES;
}

- (instancetype)init
{
  self = [super init];
  if (self) {
    [[NSNotificationCenter defaultCenter]
        addObserver:self
           selector:@selector(handleUnityEventNotification:)
               name:@"RippiesUnityRevealEventNotification"
             object:nil];
  }
  return self;
}

- (void)dealloc
{
  [[NSNotificationCenter defaultCenter] removeObserver:self];
}

- (NSArray<NSString *> *)supportedEvents
{
  return @[ @"RippiesUnityRevealEvent" ];
}

- (NSDictionary *)constantsToExport
{
  return @{ @"isAvailable" : @([self unityFrameworkClass] != Nil) };
}

- (Class)unityFrameworkClass
{
  Class frameworkClass = NSClassFromString(@"UnityFramework");
  if (frameworkClass != Nil) {
    return frameworkClass;
  }

  NSString *frameworkPath = [[NSBundle mainBundle].privateFrameworksPath
      stringByAppendingPathComponent:@"UnityFramework.framework"];
  NSBundle *frameworkBundle = [NSBundle bundleWithPath:frameworkPath];
  if (frameworkBundle && !frameworkBundle.loaded) {
    NSError *error = nil;
    [frameworkBundle loadAndReturnError:&error];
    if (error) {
      NSLog(@"[RippiesUnity] UnityFramework load failed: %@", error);
    }
  }
  return NSClassFromString(@"UnityFramework");
}

- (BOOL)prepareUnity:(NSError **)error
{
  Class frameworkClass = [self unityFrameworkClass];
  if (frameworkClass == Nil) {
    if (error) {
      *error = [NSError
          errorWithDomain:@"RippiesUnity"
                     code:1
                 userInfo:@{
                   NSLocalizedDescriptionKey :
                       @"UnityFramework.framework is not embedded. Run the local iOS Unity export workflow."
                 }];
    }
    return NO;
  }

  if (!self.unityFramework) {
    SEL getInstance = NSSelectorFromString(@"getInstance");
    self.unityFramework =
        ((id(*)(id, SEL))objc_msgSend)(frameworkClass, getInstance);
  }
  if (!self.unityFramework) {
    return NO;
  }

  SEL appControllerSelector = NSSelectorFromString(@"appController");
  id appController = ((id(*)(id, SEL))objc_msgSend)(
      self.unityFramework, appControllerSelector);
  if (!appController) {
    id appDelegate = UIApplication.sharedApplication.delegate;
    if ([appDelegate respondsToSelector:@selector(window)]) {
      self.reactWindow = ((UIWindow * (*)(id, SEL))objc_msgSend)(
          appDelegate, @selector(window));
    }

    SEL setDataBundle = NSSelectorFromString(@"setDataBundleId:");
    ((void (*)(id, SEL, const char *))objc_msgSend)(
        self.unityFramework, setDataBundle, "com.unity3d.framework");

    NSArray<NSString *> *arguments = NSProcessInfo.processInfo.arguments;
    int argc = (int)arguments.count;
    char **argv = (char **)calloc((size_t)argc, sizeof(char *));
    for (NSInteger index = 0; index < arguments.count; index++) {
      argv[index] = strdup(arguments[index].UTF8String);
    }

    SEL runEmbedded =
        NSSelectorFromString(@"runEmbeddedWithArgc:argv:appLaunchOpts:");
    NSDictionary *launchOptions = @{};
    ((void (*)(id, SEL, int, char **, NSDictionary *))objc_msgSend)(
        self.unityFramework, runEmbedded, argc, argv, launchOptions);

    for (NSInteger index = 0; index < arguments.count; index++) {
      free(argv[index]);
    }
    free(argv);

    // Unity can render while the React Native window remains visible. The
    // sceneReady event promotes the Unity window for the seamless handoff.
    [self.reactWindow makeKeyAndVisible];
  }
  return YES;
}

- (BOOL)sendMessage:(NSString *)method
              value:(NSString *)value
              error:(NSError **)error
{
  if (![self prepareUnity:error]) {
    return NO;
  }

  SEL sendMessageSelector =
      NSSelectorFromString(@"sendMessageToGOWithName:functionName:message:");
  if (![self.unityFramework respondsToSelector:sendMessageSelector]) {
    if (error) {
      *error = [NSError
          errorWithDomain:@"RippiesUnity"
                     code:2
                 userInfo:@{
                   NSLocalizedDescriptionKey :
                       @"UnitySendMessage is unavailable in the embedded Unity framework."
                 }];
    }
    return NO;
  }

  ((void (*)(id, SEL, const char *, const char *, const char *))objc_msgSend)(
      self.unityFramework,
      sendMessageSelector,
      "NativeRevealBridge",
      method.UTF8String,
      (value ?: @"").UTF8String);
  return YES;
}

- (UIWindow *)unityWindow
{
  SEL appControllerSelector = NSSelectorFromString(@"appController");
  id appController = ((id(*)(id, SEL))objc_msgSend)(
      self.unityFramework, appControllerSelector);
  if ([appController respondsToSelector:@selector(window)]) {
    return ((UIWindow * (*)(id, SEL))objc_msgSend)(
        appController, @selector(window));
  }
  return nil;
}

- (void)showUnityWindow
{
  if (!self.unityFramework) {
    return;
  }
  SEL showWindow = NSSelectorFromString(@"showUnityWindow");
  if ([self.unityFramework respondsToSelector:showWindow]) {
    ((void (*)(id, SEL))objc_msgSend)(self.unityFramework, showWindow);
  }
  UIWindow *window = [self unityWindow];
  window.alpha = 0.0;
  [UIView animateWithDuration:0.36
      delay:0.0
      options:UIViewAnimationOptionCurveEaseInOut
                   animations:^{
                     window.alpha = 1.0;
                   }
      completion:nil];
}

- (void)restoreReactWindow
{
  self.reactWindow.alpha = 1.0;
  [self.reactWindow makeKeyAndVisible];
}

- (void)restoreReactWindowAnimated
{
  UIWindow *unityWindow = [self unityWindow];
  self.returningToReact = YES;
  [self.reactWindow makeKeyAndVisible];
  self.reactWindow.alpha = 0.0;
  [UIView animateWithDuration:0.36
      delay:0.0
      options:UIViewAnimationOptionCurveEaseInOut
      animations:^{
        self.reactWindow.alpha = 1.0;
        unityWindow.alpha = 0.0;
      }
      completion:^(__unused BOOL finished) {
        unityWindow.alpha = 1.0;
        self.returningToReact = NO;
      }];
}

RCT_REMAP_METHOD(
    prepareReveal,
    prepareRevealWithPayload : (NSString *)payloadJson resolver
    : (RCTPromiseResolveBlock)resolve rejecter
    : (RCTPromiseRejectBlock)reject)
{
  dispatch_async(dispatch_get_main_queue(), ^{
    NSData *payloadData = [payloadJson dataUsingEncoding:NSUTF8StringEncoding];
    NSDictionary *payload = payloadData
        ? [NSJSONSerialization JSONObjectWithData:payloadData
                                         options:0
                                           error:nil]
        : nil;
    self.pendingRevealPayload = payloadJson;
    self.returningToReact = NO;
    self.pendingRevealId =
        [payload[@"revealId"] isKindOfClass:NSString.class]
        ? payload[@"revealId"]
        : nil;

    NSError *error = nil;
    if (![self prepareUnity:&error]) {
      reject(@"unity_unavailable", error.localizedDescription, error);
      return;
    }

    if (self.unitySceneLoaded) {
      NSLog(@"[RippiesUnity] Sending reveal payload to loaded scene");
      if (![self sendMessage:@"PrepareReveal"
                       value:self.pendingRevealPayload
                       error:&error]) {
        reject(@"unity_message_failed", error.localizedDescription, error);
        return;
      }
      self.pendingRevealPayload = nil;
    }
    resolve(nil);
  });
}

RCT_REMAP_METHOD(
    beginReveal,
    beginRevealWithResolver : (RCTPromiseResolveBlock)resolve rejecter
    : (RCTPromiseRejectBlock)reject)
{
  [self invoke:@"BeginReveal" value:@"" resolve:resolve reject:reject];
}

RCT_REMAP_METHOD(
    skipReveal,
    skipRevealWithResolver : (RCTPromiseResolveBlock)resolve rejecter
    : (RCTPromiseRejectBlock)reject)
{
  [self invoke:@"SkipReveal" value:@"" resolve:resolve reject:reject];
}

RCT_REMAP_METHOD(
    pauseReveal,
    pauseRevealWithValue : (BOOL)paused resolver
    : (RCTPromiseResolveBlock)resolve rejecter
    : (RCTPromiseRejectBlock)reject)
{
  [self
      invoke:@"PauseReveal"
       value:paused ? @"true" : @"false"
     resolve:resolve
      reject:reject];
}

RCT_REMAP_METHOD(
    setMuted,
    setMutedWithValue : (BOOL)muted resolver
    : (RCTPromiseResolveBlock)resolve rejecter
    : (RCTPromiseRejectBlock)reject)
{
  [self
      invoke:@"SetMuted"
       value:muted ? @"true" : @"false"
     resolve:resolve
      reject:reject];
}

RCT_REMAP_METHOD(
    disposeReveal,
    disposeRevealWithResolver : (RCTPromiseResolveBlock)resolve rejecter
    : (RCTPromiseRejectBlock)reject)
{
  dispatch_async(dispatch_get_main_queue(), ^{
    if (self.returningToReact) {
      // Preserve Unity's closing pose during the native crossfade, then park
      // the scene after it is no longer visible.
      resolve(nil);
      dispatch_after(
          dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.4 * NSEC_PER_SEC)),
          dispatch_get_main_queue(),
          ^{
            NSError *ignoredError = nil;
            [self sendMessage:@"DisposeReveal" value:@"" error:&ignoredError];
          });
      return;
    }

    NSError *error = nil;
    if ([self sendMessage:@"DisposeReveal" value:@"" error:&error]) {
      resolve(nil);
      [self restoreReactWindowAnimated];
    } else {
      reject(@"unity_message_failed", error.localizedDescription, error);
    }
  });
}

- (void)invoke:(NSString *)method
         value:(NSString *)value
       resolve:(RCTPromiseResolveBlock)resolve
        reject:(RCTPromiseRejectBlock)reject
{
  dispatch_async(dispatch_get_main_queue(), ^{
    NSError *error = nil;
    if ([self sendMessage:method value:value error:&error]) {
      resolve(nil);
    } else {
      reject(@"unity_message_failed", error.localizedDescription, error);
    }
  });
}

- (void)receiveUnityEvent:(NSString *)payload
{
  NSLog(@"[RippiesUnity] Native host received %@", payload);
  NSData *data = [payload dataUsingEncoding:NSUTF8StringEncoding];
  NSDictionary *event = data
      ? [NSJSONSerialization JSONObjectWithData:data options:0 error:nil]
      : nil;
  if (![event isKindOfClass:NSDictionary.class]) {
    return;
  }

  NSString *eventName = event[@"eventName"];
  if ([eventName isEqualToString:@"sceneReady"]) {
    self.unitySceneLoaded = YES;
    if (self.pendingRevealPayload &&
        ![event[@"value"] isEqualToString:self.pendingRevealId]) {
      NSError *error = nil;
      NSString *queuedPayload = self.pendingRevealPayload;
      self.pendingRevealPayload = nil;
      NSLog(@"[RippiesUnity] Sending queued reveal payload after initial scene");
      if (![self sendMessage:@"PrepareReveal" value:queuedPayload error:&error]) {
        NSLog(@"[RippiesUnity] Queued reveal failed: %@", error);
      }
      return;
    }
    [self showUnityWindow];
  }
  [self sendEventWithName:@"RippiesUnityRevealEvent" body:event];
  if ([eventName isEqualToString:@"collectionRequested"]) {
    [self restoreReactWindowAnimated];
  }
}

- (void)handleUnityEventNotification:(NSNotification *)notification
{
  NSString *payload = notification.userInfo[@"payload"];
  if ([payload isKindOfClass:NSString.class]) {
    [self receiveUnityEvent:payload];
  }
}

@end
