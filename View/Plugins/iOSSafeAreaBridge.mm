#import <UIKit/UIKit.h>

// Define the struct to hold both safe area insets and window size
typedef struct __attribute__((packed)) SafeAreaData {
    Float32 top;
    Float32 bottom;
    Float32 left;
    Float32 right;
    Float32 width;
    Float32 height;
} SafeAreaData;

// Ensure C linkage for the function
extern "C" {

SafeAreaData GetIOSSafeAreaData() {
    SafeAreaData data = {0, 0, 0, 0, 0, 0};
    UIWindow *window = UIApplication.sharedApplication.windows.firstObject;

    if (window) {
        data.top = (Float32)window.safeAreaInsets.top;
        data.bottom = (Float32)window.safeAreaInsets.bottom;
        data.left = (Float32)window.safeAreaInsets.left;
        data.right = (Float32)window.safeAreaInsets.right;
        data.width = (Float32)window.bounds.size.width;
        data.height = (Float32)window.bounds.size.height;
    } else {
        NSLog(@"[iOS SafeArea] No windows found.");
    }

    return data;
}

}