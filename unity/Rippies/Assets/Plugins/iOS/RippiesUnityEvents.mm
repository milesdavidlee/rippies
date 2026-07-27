#import <Foundation/Foundation.h>

extern "C" __attribute__((visibility("default"))) void
RippiesUnityEvent(const char *payload)
{
    if (payload == nullptr)
    {
        return;
    }

    NSString *json = [NSString stringWithUTF8String:payload];
    if (json == nil)
    {
        return;
    }

    NSLog(@"[RippiesUnity] Unity emitted %@", json);
    dispatch_async(dispatch_get_main_queue(), ^{
        [[NSNotificationCenter defaultCenter]
            postNotificationName:@"RippiesUnityRevealEventNotification"
                          object:nil
                        userInfo:@{@"payload" : json}];
    });
}
