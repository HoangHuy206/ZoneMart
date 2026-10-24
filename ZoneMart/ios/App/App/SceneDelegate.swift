import UIKit
import Capacitor
import WebKit
import LocalAuthentication

class MainViewController: CAPBridgeViewController, WKScriptMessageHandler {
    override func webViewConfiguration(for instanceConfiguration: InstanceConfiguration) -> WKWebViewConfiguration {
        let config = super.webViewConfiguration(for: instanceConfiguration)
        config.userContentController.removeScriptMessageHandler(forName: "nativeFaceId")
        config.userContentController.add(self, name: "nativeFaceId")
        return config
    }

    override func viewDidLoad() {
        super.viewDidLoad()
        self.webView?.configuration.userContentController.removeScriptMessageHandler(forName: "nativeFaceId")
        self.webView?.configuration.userContentController.add(self, name: "nativeFaceId")
    }

    func userContentController(_ userContentController: WKUserContentController, didReceive message: WKScriptMessage) {
        if message.name == "nativeFaceId" {
            let context = LAContext()
            var error: NSError?
            let reason = "Xác thực Face ID để đăng nhập tài khoản ZoneMart"

            let canUseBiometrics = context.canEvaluatePolicy(.deviceOwnerAuthenticationWithBiometrics, error: &error)
            let policy: LAPolicy = canUseBiometrics ? .deviceOwnerAuthenticationWithBiometrics : .deviceOwnerAuthentication

            context.evaluatePolicy(policy, localizedReason: reason) { success, authError in
                DispatchQueue.main.async {
                    if success {
                        self.webView?.evaluateJavaScript("if (window.__handleNativeFaceIdResult) { window.__handleNativeFaceIdResult(true, ''); }")
                    } else {
                        let desc = authError?.localizedDescription ?? "Xác thực Face ID không khớp hoặc đã bị hủy"
                        let safeDesc = desc.replacingOccurrences(of: "'", with: "\\'").replacingOccurrences(of: "\n", with: " ")
                        self.webView?.evaluateJavaScript("if (window.__handleNativeFaceIdResult) { window.__handleNativeFaceIdResult(false, '\(safeDesc)'); }")
                    }
                }
            }
        }
    }
}

class SceneDelegate: UIResponder, UIWindowSceneDelegate {
    var window: UIWindow?

    func scene(_ scene: UIScene, willConnectTo session: UISceneSession, options connectionOptions: UIScene.ConnectionOptions) {
        guard let windowScene = scene as? UIWindowScene else { return }

        window = UIWindow(windowScene: windowScene)
        window?.rootViewController = MainViewController()
        window?.makeKeyAndVisible()

        SceneDelegateProxy.shared.scene(scene, willConnectTo: session, options: connectionOptions)
    }

    func scene(_ scene: UIScene, openURLContexts URLContexts: Set<UIOpenURLContext>) {
        SceneDelegateProxy.shared.scene(scene, openURLContexts: URLContexts)
    }

    func scene(_ scene: UIScene, continue userActivity: NSUserActivity) {
        SceneDelegateProxy.shared.scene(scene, continue: userActivity)
    }
}
