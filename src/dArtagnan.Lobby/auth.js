import passport from 'passport';
import { Strategy as GoogleStrategy } from 'passport-google-oauth20';

// Google OAuth 설정
passport.use(new GoogleStrategy({
    clientID: process.env.GOOGLE_CLIENT_ID,
    clientSecret: process.env.GOOGLE_CLIENT_SECRET,
    callbackURL: process.env.GOOGLE_CALLBACK_URL || "https://dartagnan.shop/auth/google/callback"
}, async (accessToken, refreshToken, profile, done) => {
    // 구글에서 받은 사용자 정보
    const user = {
        provider: 'google',
        providerId: profile.id,
        email: profile.emails?.[0]?.value,
        name: profile.displayName,
        picture: profile.photos?.[0]?.value
    };
    
    return done(null, user);
}));

// 세션 직렬화 (간단하게 사용자 객체 그대로 저장)
passport.serializeUser((user, done) => {
    done(null, user);
});

passport.deserializeUser((user, done) => {
    done(null, user);
});

export default passport;