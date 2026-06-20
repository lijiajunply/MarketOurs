import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_de.dart';
import 'app_localizations_en.dart';
import 'app_localizations_fr.dart';
import 'app_localizations_ja.dart';
import 'app_localizations_ko.dart';
import 'app_localizations_ru.dart';
import 'app_localizations_zh.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('de'),
    Locale('en'),
    Locale('fr'),
    Locale('ja'),
    Locale('ko'),
    Locale('ru'),
    Locale('zh'),
  ];

  /// App name / title
  ///
  /// In zh, this message translates to:
  /// **'光汇'**
  String get appTitle;

  /// Campus marketplace slogan
  ///
  /// In zh, this message translates to:
  /// **'校园集市'**
  String get appSlogan;

  /// Follow system locale option
  ///
  /// In zh, this message translates to:
  /// **'跟随系统'**
  String get followSystem;

  /// No description provided for @cancel.
  ///
  /// In zh, this message translates to:
  /// **'取消'**
  String get cancel;

  /// No description provided for @ok.
  ///
  /// In zh, this message translates to:
  /// **'确定'**
  String get ok;

  /// No description provided for @confirm.
  ///
  /// In zh, this message translates to:
  /// **'确认'**
  String get confirm;

  /// No description provided for @retry.
  ///
  /// In zh, this message translates to:
  /// **'重试'**
  String get retry;

  /// No description provided for @reload.
  ///
  /// In zh, this message translates to:
  /// **'重新加载'**
  String get reload;

  /// No description provided for @save.
  ///
  /// In zh, this message translates to:
  /// **'保存'**
  String get save;

  /// No description provided for @delete.
  ///
  /// In zh, this message translates to:
  /// **'删除'**
  String get delete;

  /// No description provided for @edit.
  ///
  /// In zh, this message translates to:
  /// **'编辑'**
  String get edit;

  /// No description provided for @search.
  ///
  /// In zh, this message translates to:
  /// **'搜索'**
  String get search;

  /// No description provided for @submit.
  ///
  /// In zh, this message translates to:
  /// **'提交'**
  String get submit;

  /// No description provided for @back.
  ///
  /// In zh, this message translates to:
  /// **'返回'**
  String get back;

  /// No description provided for @close.
  ///
  /// In zh, this message translates to:
  /// **'关闭'**
  String get close;

  /// No description provided for @done.
  ///
  /// In zh, this message translates to:
  /// **'完成'**
  String get done;

  /// No description provided for @next.
  ///
  /// In zh, this message translates to:
  /// **'下一步'**
  String get next;

  /// No description provided for @yes.
  ///
  /// In zh, this message translates to:
  /// **'是'**
  String get yes;

  /// No description provided for @no.
  ///
  /// In zh, this message translates to:
  /// **'否'**
  String get no;

  /// No description provided for @themeLight.
  ///
  /// In zh, this message translates to:
  /// **'亮色'**
  String get themeLight;

  /// No description provided for @themeDark.
  ///
  /// In zh, this message translates to:
  /// **'暗色'**
  String get themeDark;

  /// No description provided for @themeSystem.
  ///
  /// In zh, this message translates to:
  /// **'跟随系统'**
  String get themeSystem;

  /// No description provided for @themeMode.
  ///
  /// In zh, this message translates to:
  /// **'主题模式'**
  String get themeMode;

  /// No description provided for @themeSubtitle.
  ///
  /// In zh, this message translates to:
  /// **'切换浅色、深色或跟随系统'**
  String get themeSubtitle;

  /// No description provided for @themeHint.
  ///
  /// In zh, this message translates to:
  /// **'点击切换显示模式'**
  String get themeHint;

  /// No description provided for @appearanceModeTitle.
  ///
  /// In zh, this message translates to:
  /// **'选择外观模式'**
  String get appearanceModeTitle;

  /// No description provided for @tabHome.
  ///
  /// In zh, this message translates to:
  /// **'首页'**
  String get tabHome;

  /// No description provided for @tabHot.
  ///
  /// In zh, this message translates to:
  /// **'热榜'**
  String get tabHot;

  /// No description provided for @tabNotifications.
  ///
  /// In zh, this message translates to:
  /// **'通知'**
  String get tabNotifications;

  /// No description provided for @tabProfile.
  ///
  /// In zh, this message translates to:
  /// **'我的'**
  String get tabProfile;

  /// No description provided for @authLogin.
  ///
  /// In zh, this message translates to:
  /// **'登录'**
  String get authLogin;

  /// No description provided for @authRegister.
  ///
  /// In zh, this message translates to:
  /// **'注册账号'**
  String get authRegister;

  /// No description provided for @authForgotPassword.
  ///
  /// In zh, this message translates to:
  /// **'找回密码'**
  String get authForgotPassword;

  /// No description provided for @authResetPassword.
  ///
  /// In zh, this message translates to:
  /// **'重置密码'**
  String get authResetPassword;

  /// No description provided for @authChangePassword.
  ///
  /// In zh, this message translates to:
  /// **'修改密码'**
  String get authChangePassword;

  /// No description provided for @authLogout.
  ///
  /// In zh, this message translates to:
  /// **'退出登录'**
  String get authLogout;

  /// No description provided for @authLogoutDesc.
  ///
  /// In zh, this message translates to:
  /// **'清除当前会话'**
  String get authLogoutDesc;

  /// No description provided for @authAccount.
  ///
  /// In zh, this message translates to:
  /// **'账号'**
  String get authAccount;

  /// No description provided for @authPassword.
  ///
  /// In zh, this message translates to:
  /// **'密码'**
  String get authPassword;

  /// No description provided for @authVerificationCode.
  ///
  /// In zh, this message translates to:
  /// **'验证码'**
  String get authVerificationCode;

  /// No description provided for @authSendCode.
  ///
  /// In zh, this message translates to:
  /// **'发送验证码'**
  String get authSendCode;

  /// No description provided for @authAccountPlaceholder.
  ///
  /// In zh, this message translates to:
  /// **'账号 / 邮箱 / 手机号'**
  String get authAccountPlaceholder;

  /// No description provided for @authPasswordPlaceholder.
  ///
  /// In zh, this message translates to:
  /// **'请输入密码'**
  String get authPasswordPlaceholder;

  /// No description provided for @authCodePlaceholder.
  ///
  /// In zh, this message translates to:
  /// **'6 位验证码'**
  String get authCodePlaceholder;

  /// No description provided for @authNamePlaceholder.
  ///
  /// In zh, this message translates to:
  /// **'给自己起个名字'**
  String get authNamePlaceholder;

  /// No description provided for @authAccountHint.
  ///
  /// In zh, this message translates to:
  /// **'邮箱或手机号'**
  String get authAccountHint;

  /// No description provided for @authNewPassword.
  ///
  /// In zh, this message translates to:
  /// **'新密码'**
  String get authNewPassword;

  /// No description provided for @authOldPassword.
  ///
  /// In zh, this message translates to:
  /// **'当前密码'**
  String get authOldPassword;

  /// No description provided for @authConfirmPassword.
  ///
  /// In zh, this message translates to:
  /// **'确认密码'**
  String get authConfirmPassword;

  /// No description provided for @authPasswordRequirement.
  ///
  /// In zh, this message translates to:
  /// **'至少6位，含大写、小写字母和数字'**
  String get authPasswordRequirement;

  /// No description provided for @authCodeLogin.
  ///
  /// In zh, this message translates to:
  /// **'验证码登录'**
  String get authCodeLogin;

  /// No description provided for @authPasswordLogin.
  ///
  /// In zh, this message translates to:
  /// **'密码登录'**
  String get authPasswordLogin;

  /// No description provided for @authAlreadyHaveAccount.
  ///
  /// In zh, this message translates to:
  /// **'已有账号？返回登录'**
  String get authAlreadyHaveAccount;

  /// No description provided for @authNoAccount.
  ///
  /// In zh, this message translates to:
  /// **'还没有账号？去注册'**
  String get authNoAccount;

  /// No description provided for @authForgotPasswordPrompt.
  ///
  /// In zh, this message translates to:
  /// **'忘记密码？'**
  String get authForgotPasswordPrompt;

  /// No description provided for @authSendCodeSuccess.
  ///
  /// In zh, this message translates to:
  /// **'验证码已发送'**
  String get authSendCodeSuccess;

  /// No description provided for @authSendCodeFailed.
  ///
  /// In zh, this message translates to:
  /// **'发送验证码失败，请稍后重试'**
  String get authSendCodeFailed;

  /// No description provided for @authLoginSuccess.
  ///
  /// In zh, this message translates to:
  /// **'登录成功'**
  String get authLoginSuccess;

  /// No description provided for @authLoginFailed.
  ///
  /// In zh, this message translates to:
  /// **'登录失败，请稍后重试'**
  String get authLoginFailed;

  /// No description provided for @authRegisterSuccess.
  ///
  /// In zh, this message translates to:
  /// **'注册成功'**
  String get authRegisterSuccess;

  /// No description provided for @authRegisterFailed.
  ///
  /// In zh, this message translates to:
  /// **'注册失败，请稍后重试'**
  String get authRegisterFailed;

  /// No description provided for @authResetFailed.
  ///
  /// In zh, this message translates to:
  /// **'重置密码失败，请稍后重试'**
  String get authResetFailed;

  /// No description provided for @authChangePasswordFailed.
  ///
  /// In zh, this message translates to:
  /// **'修改密码失败，请稍后重试'**
  String get authChangePasswordFailed;

  /// No description provided for @authVerifyFailed.
  ///
  /// In zh, this message translates to:
  /// **'验证失败，请稍后重试'**
  String get authVerifyFailed;

  /// No description provided for @authUsernameOrPasswordWrong.
  ///
  /// In zh, this message translates to:
  /// **'用户名或密码错误'**
  String get authUsernameOrPasswordWrong;

  /// No description provided for @authRegisterVerifyTitle.
  ///
  /// In zh, this message translates to:
  /// **'验证注册'**
  String get authRegisterVerifyTitle;

  /// No description provided for @authRegisterVerifyHint.
  ///
  /// In zh, this message translates to:
  /// **'请输入发送到您账号的验证码'**
  String get authRegisterVerifyHint;

  /// No description provided for @authSelectAvatar.
  ///
  /// In zh, this message translates to:
  /// **'选择头像'**
  String get authSelectAvatar;

  /// No description provided for @authRandomAvatar.
  ///
  /// In zh, this message translates to:
  /// **'随机生成'**
  String get authRandomAvatar;

  /// No description provided for @authPickFromGallery.
  ///
  /// In zh, this message translates to:
  /// **'从相册选择'**
  String get authPickFromGallery;

  /// No description provided for @authTakePhoto.
  ///
  /// In zh, this message translates to:
  /// **'拍照'**
  String get authTakePhoto;

  /// No description provided for @authResendCode.
  ///
  /// In zh, this message translates to:
  /// **'重新发送'**
  String get authResendCode;

  /// No description provided for @profileInfo.
  ///
  /// In zh, this message translates to:
  /// **'资料信息'**
  String get profileInfo;

  /// No description provided for @profileSecurity.
  ///
  /// In zh, this message translates to:
  /// **'账户安全'**
  String get profileSecurity;

  /// No description provided for @profileSocial.
  ///
  /// In zh, this message translates to:
  /// **'社交管理'**
  String get profileSocial;

  /// No description provided for @profileBindings.
  ///
  /// In zh, this message translates to:
  /// **'第三方绑定'**
  String get profileBindings;

  /// No description provided for @profileNickname.
  ///
  /// In zh, this message translates to:
  /// **'昵称'**
  String get profileNickname;

  /// No description provided for @profileBio.
  ///
  /// In zh, this message translates to:
  /// **'简介'**
  String get profileBio;

  /// No description provided for @profileEmail.
  ///
  /// In zh, this message translates to:
  /// **'邮箱'**
  String get profileEmail;

  /// No description provided for @profilePhone.
  ///
  /// In zh, this message translates to:
  /// **'手机'**
  String get profilePhone;

  /// No description provided for @profileEmailVerified.
  ///
  /// In zh, this message translates to:
  /// **'已通过安全验证'**
  String get profileEmailVerified;

  /// No description provided for @profileEmailNotVerified.
  ///
  /// In zh, this message translates to:
  /// **'尚未进行安全验证'**
  String get profileEmailNotVerified;

  /// No description provided for @profileVerifyEmail.
  ///
  /// In zh, this message translates to:
  /// **'去验证'**
  String get profileVerifyEmail;

  /// No description provided for @profileVerifyEmailTitle.
  ///
  /// In zh, this message translates to:
  /// **'邮箱验证'**
  String get profileVerifyEmailTitle;

  /// No description provided for @profileEnterCode.
  ///
  /// In zh, this message translates to:
  /// **'输入验证码'**
  String get profileEnterCode;

  /// No description provided for @profileEnterCodeHint.
  ///
  /// In zh, this message translates to:
  /// **'请输入收到的验证码'**
  String get profileEnterCodeHint;

  /// No description provided for @profileConfirmVerify.
  ///
  /// In zh, this message translates to:
  /// **'确认验证'**
  String get profileConfirmVerify;

  /// No description provided for @profileNotLoggedIn.
  ///
  /// In zh, this message translates to:
  /// **'还没有登录'**
  String get profileNotLoggedIn;

  /// No description provided for @profileNotLoggedInDesc.
  ///
  /// In zh, this message translates to:
  /// **'登录后可以查看个人资料、管理安全设置。'**
  String get profileNotLoggedInDesc;

  /// No description provided for @profileGoLogin.
  ///
  /// In zh, this message translates to:
  /// **'去登录'**
  String get profileGoLogin;

  /// No description provided for @profileEditProfile.
  ///
  /// In zh, this message translates to:
  /// **'编辑资料'**
  String get profileEditProfile;

  /// No description provided for @profileSaveChanges.
  ///
  /// In zh, this message translates to:
  /// **'保存修改'**
  String get profileSaveChanges;

  /// No description provided for @profileSaving.
  ///
  /// In zh, this message translates to:
  /// **'保存中...'**
  String get profileSaving;

  /// No description provided for @profileUpdated.
  ///
  /// In zh, this message translates to:
  /// **'个人资料已更新'**
  String get profileUpdated;

  /// No description provided for @profileUpdateFailed.
  ///
  /// In zh, this message translates to:
  /// **'保存失败，请稍后重试'**
  String get profileUpdateFailed;

  /// No description provided for @profileNoNickname.
  ///
  /// In zh, this message translates to:
  /// **'未设置昵称'**
  String get profileNoNickname;

  /// No description provided for @profileNoBio.
  ///
  /// In zh, this message translates to:
  /// **'还没有写简介'**
  String get profileNoBio;

  /// No description provided for @profileNoEmail.
  ///
  /// In zh, this message translates to:
  /// **'未绑定'**
  String get profileNoEmail;

  /// No description provided for @profileClickToChangeAvatar.
  ///
  /// In zh, this message translates to:
  /// **'点击更换头像'**
  String get profileClickToChangeAvatar;

  /// No description provided for @profileChangePasswordTitle.
  ///
  /// In zh, this message translates to:
  /// **'修改密码'**
  String get profileChangePasswordTitle;

  /// No description provided for @profileChangePasswordDesc.
  ///
  /// In zh, this message translates to:
  /// **'更新当前账号密码'**
  String get profileChangePasswordDesc;

  /// No description provided for @profileFollowBlock.
  ///
  /// In zh, this message translates to:
  /// **'关注与屏蔽'**
  String get profileFollowBlock;

  /// No description provided for @profileFollowBlockDesc.
  ///
  /// In zh, this message translates to:
  /// **'管理关注的用户和屏蔽列表'**
  String get profileFollowBlockDesc;

  /// No description provided for @profileAbout.
  ///
  /// In zh, this message translates to:
  /// **'关于'**
  String get profileAbout;

  /// No description provided for @profileTerms.
  ///
  /// In zh, this message translates to:
  /// **'用户协议'**
  String get profileTerms;

  /// No description provided for @profileTermsDesc.
  ///
  /// In zh, this message translates to:
  /// **'查看平台使用条款'**
  String get profileTermsDesc;

  /// No description provided for @profilePrivacy.
  ///
  /// In zh, this message translates to:
  /// **'隐私条款'**
  String get profilePrivacy;

  /// No description provided for @profilePrivacyDesc.
  ///
  /// In zh, this message translates to:
  /// **'了解我们如何保护你的信息'**
  String get profilePrivacyDesc;

  /// No description provided for @profileDisplaySettings.
  ///
  /// In zh, this message translates to:
  /// **'显示设置'**
  String get profileDisplaySettings;

  /// No description provided for @profileManageSocialDesc.
  ///
  /// In zh, this message translates to:
  /// **'管理 Github、Google 等平台关联'**
  String get profileManageSocialDesc;

  /// No description provided for @profileNavTitle.
  ///
  /// In zh, this message translates to:
  /// **'我的'**
  String get profileNavTitle;

  /// No description provided for @profilePublicProfile.
  ///
  /// In zh, this message translates to:
  /// **'用户主页'**
  String get profilePublicProfile;

  /// No description provided for @profileUnknownDate.
  ///
  /// In zh, this message translates to:
  /// **'未知'**
  String get profileUnknownDate;

  /// No description provided for @profileEmailVerifySuccess.
  ///
  /// In zh, this message translates to:
  /// **'邮箱验证成功'**
  String get profileEmailVerifySuccess;

  /// No description provided for @profileSubmitting.
  ///
  /// In zh, this message translates to:
  /// **'提交中...'**
  String get profileSubmitting;

  /// No description provided for @profileLogoutDesc.
  ///
  /// In zh, this message translates to:
  /// **'清除当前会话'**
  String get profileLogoutDesc;

  /// No description provided for @profileFollowers.
  ///
  /// In zh, this message translates to:
  /// **'粉丝'**
  String get profileFollowers;

  /// No description provided for @profileFollowing.
  ///
  /// In zh, this message translates to:
  /// **'关注'**
  String get profileFollowing;

  /// No description provided for @profileFollow.
  ///
  /// In zh, this message translates to:
  /// **'关注'**
  String get profileFollow;

  /// No description provided for @profileUnfollow.
  ///
  /// In zh, this message translates to:
  /// **'已关注'**
  String get profileUnfollow;

  /// No description provided for @profileBlock.
  ///
  /// In zh, this message translates to:
  /// **'屏蔽'**
  String get profileBlock;

  /// No description provided for @profileUnblock.
  ///
  /// In zh, this message translates to:
  /// **'已屏蔽'**
  String get profileUnblock;

  /// No description provided for @profileThisIsYou.
  ///
  /// In zh, this message translates to:
  /// **'这是你'**
  String get profileThisIsYou;

  /// No description provided for @profileManageMyProfile.
  ///
  /// In zh, this message translates to:
  /// **'管理我的资料'**
  String get profileManageMyProfile;

  /// No description provided for @profileJoinDate.
  ///
  /// In zh, this message translates to:
  /// **'加入时间'**
  String get profileJoinDate;

  /// No description provided for @profileOwnerLowkey.
  ///
  /// In zh, this message translates to:
  /// **'这个人很低调，还没有写简介。'**
  String get profileOwnerLowkey;

  /// No description provided for @profileRecentPosts.
  ///
  /// In zh, this message translates to:
  /// **'最近发布'**
  String get profileRecentPosts;

  /// No description provided for @profileRecentPostsSubtitle.
  ///
  /// In zh, this message translates to:
  /// **'看看这位同学最近在 光汇 分享了什么。'**
  String get profileRecentPostsSubtitle;

  /// No description provided for @profileNoPublicPosts.
  ///
  /// In zh, this message translates to:
  /// **'还没有公开帖子'**
  String get profileNoPublicPosts;

  /// No description provided for @profileNoFollows.
  ///
  /// In zh, this message translates to:
  /// **'还没有关注任何人'**
  String get profileNoFollows;

  /// No description provided for @profileReachedEnd.
  ///
  /// In zh, this message translates to:
  /// **'已经到底了'**
  String get profileReachedEnd;

  /// No description provided for @postCreate.
  ///
  /// In zh, this message translates to:
  /// **'发布帖子'**
  String get postCreate;

  /// No description provided for @postCreatePublishing.
  ///
  /// In zh, this message translates to:
  /// **'发布中...'**
  String get postCreatePublishing;

  /// No description provided for @postCreatePublish.
  ///
  /// In zh, this message translates to:
  /// **'发布'**
  String get postCreatePublish;

  /// No description provided for @postCreateTitle.
  ///
  /// In zh, this message translates to:
  /// **'帖子标题'**
  String get postCreateTitle;

  /// No description provided for @postCreateTitleEmpty.
  ///
  /// In zh, this message translates to:
  /// **'请输入标题'**
  String get postCreateTitleEmpty;

  /// No description provided for @postCreateTitleTooLong.
  ///
  /// In zh, this message translates to:
  /// **'标题长度不能超过 {max} 位'**
  String postCreateTitleTooLong(int max);

  /// No description provided for @postCreateContent.
  ///
  /// In zh, this message translates to:
  /// **'分享此刻的新鲜事...'**
  String get postCreateContent;

  /// No description provided for @postCreateContentEmpty.
  ///
  /// In zh, this message translates to:
  /// **'请输入内容'**
  String get postCreateContentEmpty;

  /// No description provided for @postCreateContentTooLong.
  ///
  /// In zh, this message translates to:
  /// **'内容长度不能超过 {max} 位'**
  String postCreateContentTooLong(int max);

  /// No description provided for @postCreateImages.
  ///
  /// In zh, this message translates to:
  /// **'图片'**
  String get postCreateImages;

  /// No description provided for @postCreateAddImages.
  ///
  /// In zh, this message translates to:
  /// **'添加图片'**
  String get postCreateAddImages;

  /// No description provided for @postCreateNoImages.
  ///
  /// In zh, this message translates to:
  /// **'还没选择图片'**
  String get postCreateNoImages;

  /// No description provided for @postCreateTag.
  ///
  /// In zh, this message translates to:
  /// **'标签'**
  String get postCreateTag;

  /// No description provided for @postCreateNoTag.
  ///
  /// In zh, this message translates to:
  /// **'无标签'**
  String get postCreateNoTag;

  /// No description provided for @postCreated.
  ///
  /// In zh, this message translates to:
  /// **'帖子已发布'**
  String get postCreated;

  /// No description provided for @postCreateFailed.
  ///
  /// In zh, this message translates to:
  /// **'帖子创建失败'**
  String get postCreateFailed;

  /// No description provided for @postUploading.
  ///
  /// In zh, this message translates to:
  /// **'上传中...'**
  String get postUploading;

  /// No description provided for @postImagesUploadFailed.
  ///
  /// In zh, this message translates to:
  /// **'图片上传失败'**
  String get postImagesUploadFailed;

  /// No description provided for @postDetail.
  ///
  /// In zh, this message translates to:
  /// **'详情'**
  String get postDetail;

  /// No description provided for @postDetailTitle.
  ///
  /// In zh, this message translates to:
  /// **'帖子详情'**
  String get postDetailTitle;

  /// No description provided for @postNotFound.
  ///
  /// In zh, this message translates to:
  /// **'帖子不存在'**
  String get postNotFound;

  /// No description provided for @postDeleted.
  ///
  /// In zh, this message translates to:
  /// **'帖子已删除'**
  String get postDeleted;

  /// No description provided for @postDeleteConfirm.
  ///
  /// In zh, this message translates to:
  /// **'确认删除此帖子？'**
  String get postDeleteConfirm;

  /// No description provided for @postEdit.
  ///
  /// In zh, this message translates to:
  /// **'编辑帖子'**
  String get postEdit;

  /// No description provided for @postNoComments.
  ///
  /// In zh, this message translates to:
  /// **'暂无评论'**
  String get postNoComments;

  /// No description provided for @postWriteComment.
  ///
  /// In zh, this message translates to:
  /// **'写下你的评论...'**
  String get postWriteComment;

  /// No description provided for @postCommentSent.
  ///
  /// In zh, this message translates to:
  /// **'评论已发布'**
  String get postCommentSent;

  /// No description provided for @postCommentFailed.
  ///
  /// In zh, this message translates to:
  /// **'评论发送失败'**
  String get postCommentFailed;

  /// No description provided for @postShareTitle.
  ///
  /// In zh, this message translates to:
  /// **'来自光汇的帖子'**
  String get postShareTitle;

  /// No description provided for @postShareText.
  ///
  /// In zh, this message translates to:
  /// **'来自 {name} 的帖子: {title}'**
  String postShareText(String name, String title);

  /// No description provided for @postCommentSortNewest.
  ///
  /// In zh, this message translates to:
  /// **'最新'**
  String get postCommentSortNewest;

  /// No description provided for @postCommentSortOldest.
  ///
  /// In zh, this message translates to:
  /// **'最早'**
  String get postCommentSortOldest;

  /// No description provided for @postCommentSortHot.
  ///
  /// In zh, this message translates to:
  /// **'最热'**
  String get postCommentSortHot;

  /// No description provided for @postLoadMore.
  ///
  /// In zh, this message translates to:
  /// **'加载更多'**
  String get postLoadMore;

  /// No description provided for @postLoading.
  ///
  /// In zh, this message translates to:
  /// **'加载中...'**
  String get postLoading;

  /// No description provided for @postUserNotFound.
  ///
  /// In zh, this message translates to:
  /// **'用户不存在'**
  String get postUserNotFound;

  /// No description provided for @postPleaseLogin.
  ///
  /// In zh, this message translates to:
  /// **'请先登录'**
  String get postPleaseLogin;

  /// No description provided for @postImageSaveSuccess.
  ///
  /// In zh, this message translates to:
  /// **'图片已保存到系统相册'**
  String get postImageSaveSuccess;

  /// No description provided for @postImageSaveFailed.
  ///
  /// In zh, this message translates to:
  /// **'图片保存失败'**
  String get postImageSaveFailed;

  /// No description provided for @postTagTitle.
  ///
  /// In zh, this message translates to:
  /// **'标签'**
  String get postTagTitle;

  /// No description provided for @postTagEmpty.
  ///
  /// In zh, this message translates to:
  /// **'这个标签下还没有帖子'**
  String get postTagEmpty;

  /// No description provided for @homeSearchPlaceholder.
  ///
  /// In zh, this message translates to:
  /// **'搜索帖子、话题或用户'**
  String get homeSearchPlaceholder;

  /// No description provided for @homeEmpty.
  ///
  /// In zh, this message translates to:
  /// **'还没有帖子'**
  String get homeEmpty;

  /// No description provided for @homeHotEmpty.
  ///
  /// In zh, this message translates to:
  /// **'还没有热门帖子'**
  String get homeHotEmpty;

  /// No description provided for @notificationTitle.
  ///
  /// In zh, this message translates to:
  /// **'通知'**
  String get notificationTitle;

  /// No description provided for @notificationEmpty.
  ///
  /// In zh, this message translates to:
  /// **'暂时没有通知'**
  String get notificationEmpty;

  /// No description provided for @notificationPushSettings.
  ///
  /// In zh, this message translates to:
  /// **'推送设置'**
  String get notificationPushSettings;

  /// No description provided for @notificationEmail.
  ///
  /// In zh, this message translates to:
  /// **'邮件通知'**
  String get notificationEmail;

  /// No description provided for @notificationEmailDesc.
  ///
  /// In zh, this message translates to:
  /// **'当收到新回复或系统通知时发送邮件'**
  String get notificationEmailDesc;

  /// No description provided for @notificationCommentPush.
  ///
  /// In zh, this message translates to:
  /// **'评论回复推送'**
  String get notificationCommentPush;

  /// No description provided for @notificationCommentPushDesc.
  ///
  /// In zh, this message translates to:
  /// **'当有人回复您的贴子或评论时推送'**
  String get notificationCommentPushDesc;

  /// No description provided for @notificationHotListPush.
  ///
  /// In zh, this message translates to:
  /// **'每日热榜推送'**
  String get notificationHotListPush;

  /// No description provided for @notificationHotListPushDesc.
  ///
  /// In zh, this message translates to:
  /// **'每天早晨接收校园最热贴子精选'**
  String get notificationHotListPushDesc;

  /// No description provided for @notificationSaving.
  ///
  /// In zh, this message translates to:
  /// **'保存中...'**
  String get notificationSaving;

  /// No description provided for @notificationSaveSettings.
  ///
  /// In zh, this message translates to:
  /// **'保存设置'**
  String get notificationSaveSettings;

  /// No description provided for @notificationSaved.
  ///
  /// In zh, this message translates to:
  /// **'保存成功'**
  String get notificationSaved;

  /// No description provided for @notificationSaveFailed.
  ///
  /// In zh, this message translates to:
  /// **'保存失败'**
  String get notificationSaveFailed;

  /// Notification title for comment reply type
  ///
  /// In zh, this message translates to:
  /// **'回复提醒'**
  String get notificationTypeCommentReplyTitle;

  /// No description provided for @notificationTypeCommentReplyContent.
  ///
  /// In zh, this message translates to:
  /// **'{commenterName} 回复了你: {bodySnippet}'**
  String notificationTypeCommentReplyContent(
    String commenterName,
    String bodySnippet,
  );

  /// No description provided for @notificationTypePostReplyTitle.
  ///
  /// In zh, this message translates to:
  /// **'新评论'**
  String get notificationTypePostReplyTitle;

  /// No description provided for @notificationTypePostReplyContent.
  ///
  /// In zh, this message translates to:
  /// **'{commenterName} 评论了你的帖子: {bodySnippet}'**
  String notificationTypePostReplyContent(
    String commenterName,
    String bodySnippet,
  );

  /// No description provided for @notificationTypeHotListTitle.
  ///
  /// In zh, this message translates to:
  /// **'🔥 今日校园热榜'**
  String get notificationTypeHotListTitle;

  /// No description provided for @notificationTypeHotListHeader.
  ///
  /// In zh, this message translates to:
  /// **'来看看大家都在聊什么：'**
  String get notificationTypeHotListHeader;

  /// No description provided for @notificationTypeReviewTitle.
  ///
  /// In zh, this message translates to:
  /// **'审核信息'**
  String get notificationTypeReviewTitle;

  /// No description provided for @notificationTypeReviewEntityPost.
  ///
  /// In zh, this message translates to:
  /// **'帖子'**
  String get notificationTypeReviewEntityPost;

  /// No description provided for @notificationTypeReviewEntityComment.
  ///
  /// In zh, this message translates to:
  /// **'评论'**
  String get notificationTypeReviewEntityComment;

  /// No description provided for @notificationTypeReviewApproved.
  ///
  /// In zh, this message translates to:
  /// **'您的{entity} \'\'{name}\'\' 已通过审核'**
  String notificationTypeReviewApproved(String entity, String name);

  /// No description provided for @notificationTypeReviewRejected.
  ///
  /// In zh, this message translates to:
  /// **'您的{entity} \'\'{name}\'\' 未通过审核: {reason}'**
  String notificationTypeReviewRejected(
    String entity,
    String name,
    String reason,
  );

  /// No description provided for @notificationTypeSystemTitle.
  ///
  /// In zh, this message translates to:
  /// **'系统通知'**
  String get notificationTypeSystemTitle;

  /// No description provided for @hotRank.
  ///
  /// In zh, this message translates to:
  /// **'热榜'**
  String get hotRank;

  /// No description provided for @hotWatch.
  ///
  /// In zh, this message translates to:
  /// **'{count} 热度'**
  String hotWatch(int count);

  /// No description provided for @likePost.
  ///
  /// In zh, this message translates to:
  /// **'点赞'**
  String get likePost;

  /// No description provided for @dislikePost.
  ///
  /// In zh, this message translates to:
  /// **'点踩'**
  String get dislikePost;

  /// No description provided for @verified.
  ///
  /// In zh, this message translates to:
  /// **'已验证'**
  String get verified;

  /// No description provided for @unverified.
  ///
  /// In zh, this message translates to:
  /// **'未验证'**
  String get unverified;

  /// No description provided for @dateJustNow.
  ///
  /// In zh, this message translates to:
  /// **'刚刚'**
  String get dateJustNow;

  /// No description provided for @dateMinutesAgo.
  ///
  /// In zh, this message translates to:
  /// **'{minutes}分钟前'**
  String dateMinutesAgo(int minutes);

  /// No description provided for @dateHoursAgo.
  ///
  /// In zh, this message translates to:
  /// **'{hours}小时前'**
  String dateHoursAgo(int hours);

  /// No description provided for @dateDaysAgo.
  ///
  /// In zh, this message translates to:
  /// **'{days}天前'**
  String dateDaysAgo(int days);

  /// No description provided for @dateYesterday.
  ///
  /// In zh, this message translates to:
  /// **'昨天'**
  String get dateYesterday;

  /// No description provided for @privacyPolicy.
  ///
  /// In zh, this message translates to:
  /// **'隐私政策'**
  String get privacyPolicy;

  /// No description provided for @termsOfService.
  ///
  /// In zh, this message translates to:
  /// **'服务条款'**
  String get termsOfService;

  /// No description provided for @settingsTitle.
  ///
  /// In zh, this message translates to:
  /// **'设置'**
  String get settingsTitle;

  /// No description provided for @settingsLanguage.
  ///
  /// In zh, this message translates to:
  /// **'语言'**
  String get settingsLanguage;

  /// No description provided for @settingsLanguageTitle.
  ///
  /// In zh, this message translates to:
  /// **'语言设置'**
  String get settingsLanguageTitle;

  /// No description provided for @successUploadImages.
  ///
  /// In zh, this message translates to:
  /// **'成功上传 {count} 张图片'**
  String successUploadImages(int count);

  /// No description provided for @errorGeneral.
  ///
  /// In zh, this message translates to:
  /// **'操作失败，请稍后重试'**
  String get errorGeneral;

  /// No description provided for @errorNetworkTimeout.
  ///
  /// In zh, this message translates to:
  /// **'连接服务器超时'**
  String get errorNetworkTimeout;

  /// No description provided for @errorNetworkFailed.
  ///
  /// In zh, this message translates to:
  /// **'网络连接失败'**
  String get errorNetworkFailed;

  /// No description provided for @errorRequestCancelled.
  ///
  /// In zh, this message translates to:
  /// **'请求已取消'**
  String get errorRequestCancelled;

  /// No description provided for @errorFileUnsupported.
  ///
  /// In zh, this message translates to:
  /// **'不支持的文件类型，仅限图片 (jpg, png, gif, webp)'**
  String get errorFileUnsupported;

  /// No description provided for @errorFileNotFound.
  ///
  /// In zh, this message translates to:
  /// **'文件未找到'**
  String get errorFileNotFound;

  /// No description provided for @errorFileUploadFailed.
  ///
  /// In zh, this message translates to:
  /// **'文件上传失败'**
  String get errorFileUploadFailed;

  /// No description provided for @errorAvatarUploadFailed.
  ///
  /// In zh, this message translates to:
  /// **'头像上传失败'**
  String get errorAvatarUploadFailed;

  /// No description provided for @errorServerError.
  ///
  /// In zh, this message translates to:
  /// **'服务器错误，请稍后重试'**
  String get errorServerError;

  /// No description provided for @errorMessage_k1001.
  ///
  /// In zh, this message translates to:
  /// **'请求参数格式错误'**
  String get errorMessage_k1001;

  /// No description provided for @errorMessage_k2001.
  ///
  /// In zh, this message translates to:
  /// **'用户不存在'**
  String get errorMessage_k2001;

  /// No description provided for @errorMessage_k2002.
  ///
  /// In zh, this message translates to:
  /// **'账号已存在'**
  String get errorMessage_k2002;

  /// No description provided for @errorMessage_k2003.
  ///
  /// In zh, this message translates to:
  /// **'用户不存在或密码错误'**
  String get errorMessage_k2003;

  /// No description provided for @errorMessage_k2004.
  ///
  /// In zh, this message translates to:
  /// **'账号尚未激活或已被禁用'**
  String get errorMessage_k2004;

  /// No description provided for @errorMessage_k3001.
  ///
  /// In zh, this message translates to:
  /// **'验证码无效或已过期'**
  String get errorMessage_k3001;

  /// No description provided for @errorMessage_k3002.
  ///
  /// In zh, this message translates to:
  /// **'注册信息已过期'**
  String get errorMessage_k3002;

  /// No description provided for @errorMessage_k3003.
  ///
  /// In zh, this message translates to:
  /// **'验证码无效或已过期'**
  String get errorMessage_k3003;

  /// No description provided for @errorMessage_k4001.
  ///
  /// In zh, this message translates to:
  /// **'权限不足'**
  String get errorMessage_k4001;

  /// No description provided for @errorMessage_k4002.
  ///
  /// In zh, this message translates to:
  /// **'令牌无效或已过期'**
  String get errorMessage_k4002;

  /// No description provided for @errorMessage_k5001.
  ///
  /// In zh, this message translates to:
  /// **'帖子不存在'**
  String get errorMessage_k5001;

  /// No description provided for @errorMessage_k5002.
  ///
  /// In zh, this message translates to:
  /// **'评论不存在'**
  String get errorMessage_k5002;

  /// No description provided for @errorMessage_k6001.
  ///
  /// In zh, this message translates to:
  /// **'操作失败'**
  String get errorMessage_k6001;

  /// No description provided for @errorMessage_k6002.
  ///
  /// In zh, this message translates to:
  /// **'缓存操作失败'**
  String get errorMessage_k6002;

  /// No description provided for @errorMessage_k6003.
  ///
  /// In zh, this message translates to:
  /// **'外部服务调用失败'**
  String get errorMessage_k6003;

  /// No description provided for @errorMessage_k7001.
  ///
  /// In zh, this message translates to:
  /// **'关注失败'**
  String get errorMessage_k7001;

  /// No description provided for @errorMessage_k7002.
  ///
  /// In zh, this message translates to:
  /// **'取消关注失败'**
  String get errorMessage_k7002;

  /// No description provided for @errorMessage_k7003.
  ///
  /// In zh, this message translates to:
  /// **'屏蔽失败'**
  String get errorMessage_k7003;

  /// No description provided for @errorMessage_k7004.
  ///
  /// In zh, this message translates to:
  /// **'取消屏蔽失败'**
  String get errorMessage_k7004;

  /// No description provided for @errorMessage_k8001.
  ///
  /// In zh, this message translates to:
  /// **'不支持的文件类型'**
  String get errorMessage_k8001;

  /// No description provided for @errorMessage_k8002.
  ///
  /// In zh, this message translates to:
  /// **'文件大小超限'**
  String get errorMessage_k8002;

  /// No description provided for @errorMessage_k9001.
  ///
  /// In zh, this message translates to:
  /// **'敏感词检测未通过'**
  String get errorMessage_k9001;

  /// No description provided for @errorMessage_k10001.
  ///
  /// In zh, this message translates to:
  /// **'OAuth 提供商不支持'**
  String get errorMessage_k10001;

  /// No description provided for @oauthGithub.
  ///
  /// In zh, this message translates to:
  /// **'GitHub'**
  String get oauthGithub;

  /// No description provided for @oauthGoogle.
  ///
  /// In zh, this message translates to:
  /// **'Google'**
  String get oauthGoogle;

  /// No description provided for @oauthWeixin.
  ///
  /// In zh, this message translates to:
  /// **'微信'**
  String get oauthWeixin;

  /// No description provided for @oauthOurs.
  ///
  /// In zh, this message translates to:
  /// **'校园账号'**
  String get oauthOurs;

  /// No description provided for @oauthOtherMethods.
  ///
  /// In zh, this message translates to:
  /// **'其他方式登录'**
  String get oauthOtherMethods;

  /// No description provided for @oauthBindTitle.
  ///
  /// In zh, this message translates to:
  /// **'绑定第三方账号'**
  String get oauthBindTitle;

  /// No description provided for @oauthBindSuccess.
  ///
  /// In zh, this message translates to:
  /// **'绑定成功'**
  String get oauthBindSuccess;

  /// No description provided for @oauthBindFailed.
  ///
  /// In zh, this message translates to:
  /// **'绑定失败'**
  String get oauthBindFailed;

  /// No description provided for @oauthUnbind.
  ///
  /// In zh, this message translates to:
  /// **'解绑'**
  String get oauthUnbind;

  /// No description provided for @oauthUnbindConfirm.
  ///
  /// In zh, this message translates to:
  /// **'确认解绑此第三方账号？'**
  String get oauthUnbindConfirm;

  /// No description provided for @oauthGoBind.
  ///
  /// In zh, this message translates to:
  /// **'去绑定'**
  String get oauthGoBind;

  /// No description provided for @oauthNotBound.
  ///
  /// In zh, this message translates to:
  /// **'未绑定'**
  String get oauthNotBound;

  /// No description provided for @language_en.
  ///
  /// In zh, this message translates to:
  /// **'English'**
  String get language_en;

  /// No description provided for @language_zh.
  ///
  /// In zh, this message translates to:
  /// **'简体中文'**
  String get language_zh;

  /// No description provided for @language_zh_Hant.
  ///
  /// In zh, this message translates to:
  /// **'繁體中文'**
  String get language_zh_Hant;

  /// No description provided for @language_ja.
  ///
  /// In zh, this message translates to:
  /// **'日本語'**
  String get language_ja;

  /// No description provided for @language_ru.
  ///
  /// In zh, this message translates to:
  /// **'Русский'**
  String get language_ru;

  /// No description provided for @language_fr.
  ///
  /// In zh, this message translates to:
  /// **'Français'**
  String get language_fr;

  /// No description provided for @language_de.
  ///
  /// In zh, this message translates to:
  /// **'Deutsch'**
  String get language_de;

  /// No description provided for @language_ko.
  ///
  /// In zh, this message translates to:
  /// **'한국어'**
  String get language_ko;

  /// No description provided for @validatorAccountRequired.
  ///
  /// In zh, this message translates to:
  /// **'请输入账号'**
  String get validatorAccountRequired;

  /// No description provided for @validatorPasswordRequired.
  ///
  /// In zh, this message translates to:
  /// **'请输入密码'**
  String get validatorPasswordRequired;

  /// No description provided for @validatorCodeRequired.
  ///
  /// In zh, this message translates to:
  /// **'请输入验证码'**
  String get validatorCodeRequired;

  /// No description provided for @validatorNameRequired.
  ///
  /// In zh, this message translates to:
  /// **'请输入显示名称'**
  String get validatorNameRequired;

  /// No description provided for @validatorNameTooLong.
  ///
  /// In zh, this message translates to:
  /// **'用户名长度不能超过 {max} 位'**
  String validatorNameTooLong(int max);

  /// No description provided for @validatorConfirmPasswordMismatch.
  ///
  /// In zh, this message translates to:
  /// **'两次输入的密码不一致'**
  String get validatorConfirmPasswordMismatch;

  /// No description provided for @validatorPasswordWeak.
  ///
  /// In zh, this message translates to:
  /// **'至少6位，含大写、小写字母和数字'**
  String get validatorPasswordWeak;

  /// No description provided for @validatorEmailInvalid.
  ///
  /// In zh, this message translates to:
  /// **'请输入有效的邮箱地址'**
  String get validatorEmailInvalid;

  /// No description provided for @oauthWebViewCantReceive.
  ///
  /// In zh, this message translates to:
  /// **'无法接收登录回调，请重试'**
  String get oauthWebViewCantReceive;

  /// No description provided for @oauthWebViewCantOpenBrowser.
  ///
  /// In zh, this message translates to:
  /// **'无法打开系统浏览器，请检查系统设置'**
  String get oauthWebViewCantOpenBrowser;

  /// No description provided for @oauthWebViewCantInitiate.
  ///
  /// In zh, this message translates to:
  /// **'无法发起第三方认证，请稍后重试'**
  String get oauthWebViewCantInitiate;

  /// No description provided for @oauthWebViewLoginFailed.
  ///
  /// In zh, this message translates to:
  /// **'登录失败，缺少令牌'**
  String get oauthWebViewLoginFailed;

  /// No description provided for @oauthWebViewOpenBrowser.
  ///
  /// In zh, this message translates to:
  /// **'正在打开系统浏览器'**
  String get oauthWebViewOpenBrowser;

  /// No description provided for @oauthWebViewAuthorizeInBrowser.
  ///
  /// In zh, this message translates to:
  /// **'请在浏览器中完成授权'**
  String get oauthWebViewAuthorizeInBrowser;

  /// No description provided for @oauthWebViewAutoReturn.
  ///
  /// In zh, this message translates to:
  /// **'完成后会自动回到光汇'**
  String get oauthWebViewAutoReturn;

  /// No description provided for @oauthWebViewOpening.
  ///
  /// In zh, this message translates to:
  /// **'正在打开'**
  String get oauthWebViewOpening;

  /// No description provided for @oauthWebViewReopen.
  ///
  /// In zh, this message translates to:
  /// **'重新打开'**
  String get oauthWebViewReopen;

  /// No description provided for @oauthBindProvider.
  ///
  /// In zh, this message translates to:
  /// **'绑定 {provider}'**
  String oauthBindProvider(String provider);

  /// No description provided for @oauthLoginProvider.
  ///
  /// In zh, this message translates to:
  /// **'{provider} 登录'**
  String oauthLoginProvider(String provider);

  /// No description provided for @verifyCodeSentContinue.
  ///
  /// In zh, this message translates to:
  /// **'验证码已发送，请继续重置密码'**
  String get verifyCodeSentContinue;

  /// No description provided for @loginFailedCheckAccount.
  ///
  /// In zh, this message translates to:
  /// **'登录失败，请检查账号和密码'**
  String get loginFailedCheckAccount;

  /// No description provided for @loginFailedCheckCode.
  ///
  /// In zh, this message translates to:
  /// **'登录失败，请检查验证码'**
  String get loginFailedCheckCode;

  /// No description provided for @displayName.
  ///
  /// In zh, this message translates to:
  /// **'显示名称'**
  String get displayName;

  /// No description provided for @uploadingImages.
  ///
  /// In zh, this message translates to:
  /// **'正在上传图片'**
  String get uploadingImages;

  /// No description provided for @postWatchCount.
  ///
  /// In zh, this message translates to:
  /// **'{count} 次浏览'**
  String postWatchCount(int count);

  /// No description provided for @commentBindingSuccess.
  ///
  /// In zh, this message translates to:
  /// **'绑定成功'**
  String get commentBindingSuccess;

  /// No description provided for @commentBindingSuccessRefreshFailed.
  ///
  /// In zh, this message translates to:
  /// **'绑定成功，但刷新资料失败，请稍后下拉刷新'**
  String get commentBindingSuccessRefreshFailed;

  /// No description provided for @postEditTitle.
  ///
  /// In zh, this message translates to:
  /// **'编辑帖子'**
  String get postEditTitle;

  /// No description provided for @postDeleteTitle.
  ///
  /// In zh, this message translates to:
  /// **'删除帖子'**
  String get postDeleteTitle;

  /// No description provided for @postSelectTag.
  ///
  /// In zh, this message translates to:
  /// **'选择标签'**
  String get postSelectTag;

  /// No description provided for @postTagAdminPreset.
  ///
  /// In zh, this message translates to:
  /// **'标签由管理员预设，可不选择。'**
  String get postTagAdminPreset;

  /// No description provided for @postEmptyCommentCTA.
  ///
  /// In zh, this message translates to:
  /// **'分享你的见解，成为第一个评论的人。'**
  String get postEmptyCommentCTA;

  /// No description provided for @postCommentCount.
  ///
  /// In zh, this message translates to:
  /// **'评论 {count}'**
  String postCommentCount(int count);

  /// No description provided for @passwordResetSuccess.
  ///
  /// In zh, this message translates to:
  /// **'密码已重置，请重新登录'**
  String get passwordResetSuccess;

  /// No description provided for @registerComplete.
  ///
  /// In zh, this message translates to:
  /// **'注册完成，请使用账号密码登录'**
  String get registerComplete;

  /// No description provided for @goBack.
  ///
  /// In zh, this message translates to:
  /// **'返回上一步'**
  String get goBack;

  /// No description provided for @codeSentToAccount.
  ///
  /// In zh, this message translates to:
  /// **'验证码已发送至 {account}'**
  String codeSentToAccount(String account);

  /// No description provided for @codeMinLength.
  ///
  /// In zh, this message translates to:
  /// **'验证码至少4位'**
  String get codeMinLength;

  /// No description provided for @regenerateInstructions.
  ///
  /// In zh, this message translates to:
  /// **'重新获取重置说明'**
  String get regenerateInstructions;

  /// No description provided for @passwordNewPlaceholder.
  ///
  /// In zh, this message translates to:
  /// **'新密码'**
  String get passwordNewPlaceholder;

  /// No description provided for @passwordRequirementHint.
  ///
  /// In zh, this message translates to:
  /// **'密码，至少6位，含大小写字母和数字'**
  String get passwordRequirementHint;

  /// No description provided for @noTagAvailable.
  ///
  /// In zh, this message translates to:
  /// **'当前没有可进入的标签'**
  String get noTagAvailable;

  /// No description provided for @enterTagPage.
  ///
  /// In zh, this message translates to:
  /// **'进入标签页'**
  String get enterTagPage;

  /// No description provided for @chooseTagHint.
  ///
  /// In zh, this message translates to:
  /// **'选择一个标签，查看该标签下的帖子。'**
  String get chooseTagHint;

  /// No description provided for @unnamedTag.
  ///
  /// In zh, this message translates to:
  /// **'未命名标签'**
  String get unnamedTag;

  /// No description provided for @waitFirstPoster.
  ///
  /// In zh, this message translates to:
  /// **'等第一位同学来发布内容，或者稍后再刷新看看。'**
  String get waitFirstPoster;

  /// No description provided for @tryDifferentKeyword.
  ///
  /// In zh, this message translates to:
  /// **'换个关键词试试，或者清空搜索回到首页。'**
  String get tryDifferentKeyword;

  /// No description provided for @refreshingPosts.
  ///
  /// In zh, this message translates to:
  /// **'正在刷新帖子...'**
  String get refreshingPosts;

  /// No description provided for @searching.
  ///
  /// In zh, this message translates to:
  /// **'正在搜索...'**
  String get searching;

  /// No description provided for @loadingFailed.
  ///
  /// In zh, this message translates to:
  /// **'加载失败'**
  String get loadingFailed;

  /// No description provided for @hotListEmpty.
  ///
  /// In zh, this message translates to:
  /// **'热榜暂时为空'**
  String get hotListEmpty;

  /// No description provided for @hotListEmptyDesc.
  ///
  /// In zh, this message translates to:
  /// **'等大家再热闹一点，热门帖子就会出现在这里。'**
  String get hotListEmptyDesc;

  /// No description provided for @validatorCodeRequiredToken.
  ///
  /// In zh, this message translates to:
  /// **'请输入验证码或 Token'**
  String get validatorCodeRequiredToken;

  /// No description provided for @validatorAccountTooLong.
  ///
  /// In zh, this message translates to:
  /// **'账号长度不能超过 {max} 位'**
  String validatorAccountTooLong(int max);

  /// No description provided for @validatorEmailOrPhoneRequired.
  ///
  /// In zh, this message translates to:
  /// **'请输入有效的邮箱或手机号'**
  String get validatorEmailOrPhoneRequired;

  /// No description provided for @validatorPasswordMinLength.
  ///
  /// In zh, this message translates to:
  /// **'密码至少 6 位'**
  String get validatorPasswordMinLength;

  /// No description provided for @reply.
  ///
  /// In zh, this message translates to:
  /// **'回复'**
  String get reply;

  /// No description provided for @editPostAction.
  ///
  /// In zh, this message translates to:
  /// **'编辑'**
  String get editPostAction;

  /// No description provided for @deletePostAction.
  ///
  /// In zh, this message translates to:
  /// **'删除'**
  String get deletePostAction;

  /// No description provided for @share.
  ///
  /// In zh, this message translates to:
  /// **'分享'**
  String get share;

  /// No description provided for @myProfile.
  ///
  /// In zh, this message translates to:
  /// **'我的主页'**
  String get myProfile;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) => <String>[
    'de',
    'en',
    'fr',
    'ja',
    'ko',
    'ru',
    'zh',
  ].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'de':
      return AppLocalizationsDe();
    case 'en':
      return AppLocalizationsEn();
    case 'fr':
      return AppLocalizationsFr();
    case 'ja':
      return AppLocalizationsJa();
    case 'ko':
      return AppLocalizationsKo();
    case 'ru':
      return AppLocalizationsRu();
    case 'zh':
      return AppLocalizationsZh();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
