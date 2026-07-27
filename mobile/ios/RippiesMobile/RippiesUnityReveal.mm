#import <React/RCTBridgeModule.h>
#import <React/RCTEventEmitter.h>
#import <UIKit/UIKit.h>
#import <dlfcn.h>
#import <objc/message.h>

typedef void (*UnitySendMessageFunction)(
    const char *objectName,
    const char *methodName,
    const char *message);

@interface RippiesUnityReveal : RCTEventEmitter <RCTBridgeModule>

@property(nonatomic, strong) id unityFramework;
@property(nonatomic, weak) UIWindow *reactWindow;

@end

static __weak RippiesUnityReveal *RippiesRevealEmitter;

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
    RippiesRevealEmitter = self;
  }
  return self;
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

  UnitySendMessageFunction sendMessage =
      (UnitySendMessageFunction)dlsym(RTLD_DEFAULT, "UnitySendMessage");
  if (!sendMessage) {
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

  sendMessage(
      "NativeRevealBridge", method.UTF8String, (value ?: @"").UTF8String);
  return YES;
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
}

- (void)restoreReactWindow
{
  [self.reactWindow makeKeyAndVisible];
}

RCT_REMAP_METHOD(
    prepareReveal,
    prepareRevealWithPayload : (NSString *)payloadJson resolver
    : (RCTPromiseResolveBlock)resolve rejecter
    : (RCTPromiseRejectBlock)reject)
{
  dispatch_async(dispatch_get_main_queue(), ^{
    NSError *error = nil;
    if ([self sendMessage:@"PrepareReveal" value:payloadJson error:&error]) {
      resolve(nil);
    } else {
      reject(@"unity_unavailable", error.localizedDescription, error);
    }
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
  [self invoke:@"DisposeReveal" value:@"" resolve:resolve reject:reject];
  dispatch_async(dispatch_get_main_queue(), ^{
    [self restoreReactWindow];
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
  NSData *data = [payload dataUsingEncoding:NSUTF8StringEncoding];
  NSDictionary *event = data
      ? [NSJSONSerialization JSONObjectWithData:data options:0 error:nil]
      : nil;
  if (![event isKindOfClass:NSDictionary.class]) {
    return;
  }

  if ([event[@"eventName"] isEqualToString:@"sceneReady"]) {
    [self showUnityWindow];
  }
  if ([event[@"eventName"] isEqualToString:@"revealComplete"]) {
    // React Native owns the collection receipt and final return action. Yield
    // immediately so its completion surface is visible above the same shell.
    [self restoreReactWindow];
  }
  [self sendEventWithName:@"RippiesUnityRevealEvent" body:event];
}

@end

extern "C" void RippiesUnityEvent(const char *payload)
{
  if (!payload) {
    return;
  }
  NSString *json = [NSString stringWithUTF8String:payload];
  dispatch_async(dispatch_get_main_queue(), ^{
    [RippiesRevealEmitter receiveUnityEvent:json];
  });
}
