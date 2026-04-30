#import <UIKit/UIKit.h>

extern "C" {

void _HapticLight() {
    if (@available(iOS 10.0, *)) {
        UIImpactFeedbackGenerator *g =
            [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
        [g impactOccurred];
    }
}

void _HapticMedium() {
    if (@available(iOS 10.0, *)) {
        UIImpactFeedbackGenerator *g =
            [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
        [g impactOccurred];
    }
}

void _HapticHeavy() {
    if (@available(iOS 10.0, *)) {
        UIImpactFeedbackGenerator *g =
            [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
        [g impactOccurred];
    }
}

} // extern "C"
