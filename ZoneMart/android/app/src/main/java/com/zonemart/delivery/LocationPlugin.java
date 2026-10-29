package com.zonemart.delivery;

import android.Manifest;
import android.content.Context;
import android.content.pm.PackageManager;
import android.location.Location;
import android.location.LocationListener;
import android.location.LocationManager;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;

import androidx.core.app.ActivityCompat;

import com.getcapacitor.JSObject;
import com.getcapacitor.Plugin;
import com.getcapacitor.PluginCall;
import com.getcapacitor.PluginMethod;
import com.getcapacitor.annotation.CapacitorPlugin;
import com.getcapacitor.annotation.Permission;

@CapacitorPlugin(
    name = "NativeLocation",
    permissions = {
        @Permission(
            strings = {Manifest.permission.ACCESS_FINE_LOCATION, Manifest.permission.ACCESS_COARSE_LOCATION},
            alias = "location"
        )
    }
)
public class LocationPlugin extends Plugin {

    @PluginMethod
    public void getCurrentPosition(final PluginCall call) {
        Context context = getContext();
        if (context == null) {
            call.reject("Context không khả dụng");
            return;
        }

        boolean hasFine = ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED;
        boolean hasCoarse = ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_COARSE_LOCATION) == PackageManager.PERMISSION_GRANTED;

        if (!hasFine && !hasCoarse) {
            requestPermissionForAlias("location", call, "locationPermissionCallback");
            return;
        }

        obtainLocation(call);
    }

    private void obtainLocation(final PluginCall call) {
        try {
            Context context = getContext();
            if (context == null) {
                call.reject("Context không khả dụng");
                return;
            }

            final LocationManager locationManager = (LocationManager) context.getSystemService(Context.LOCATION_SERVICE);
            if (locationManager == null) {
                call.reject("Không thể truy cập dịch vụ vị trí hệ thống.");
                return;
            }

            // 1. Kiểm tra vị trí đã biết gần nhất
            Location bestLocation = null;
            try {
                Location gps = locationManager.getLastKnownLocation(LocationManager.GPS_PROVIDER);
                Location net = locationManager.getLastKnownLocation(LocationManager.NETWORK_PROVIDER);
                Location pass = locationManager.getLastKnownLocation(LocationManager.PASSIVE_PROVIDER);

                if (gps != null) bestLocation = gps;
                if (net != null && (bestLocation == null || net.getTime() > bestLocation.getTime())) bestLocation = net;
                if (pass != null && (bestLocation == null || pass.getTime() > bestLocation.getTime())) bestLocation = pass;
            } catch (SecurityException ignored) {}

            // Nếu vị trí gần nhất còn mới (< 10 phút), trả về ngay lập tức (0ms)
            if (bestLocation != null && (System.currentTimeMillis() - bestLocation.getTime() < 600000)) {
                JSObject ret = new JSObject();
                ret.put("latitude", bestLocation.getLatitude());
                ret.put("longitude", bestLocation.getLongitude());
                ret.put("accuracy", bestLocation.getAccuracy());
                call.resolve(ret);
                return;
            }

            final boolean[] resolved = {false};
            final Location fallbackLocation = bestLocation;

            final LocationListener listener = new LocationListener() {
                @Override
                public void onLocationChanged(Location location) {
                    if (resolved[0]) return;
                    resolved[0] = true;
                    try {
                        locationManager.removeUpdates(this);
                    } catch (Exception ignored) {}

                    JSObject ret = new JSObject();
                    ret.put("latitude", location.getLatitude());
                    ret.put("longitude", location.getLongitude());
                    ret.put("accuracy", location.getAccuracy());
                    call.resolve(ret);
                }

                @Override public void onStatusChanged(String provider, int status, Bundle extras) {}
                @Override public void onProviderEnabled(String provider) {}
                @Override public void onProviderDisabled(String provider) {}
            };

            Looper looper = Looper.getMainLooper();
            boolean requested = false;
            try {
                if (locationManager.isProviderEnabled(LocationManager.NETWORK_PROVIDER)) {
                    locationManager.requestLocationUpdates(LocationManager.NETWORK_PROVIDER, 1000L, 0f, listener, looper);
                    requested = true;
                }
                if (locationManager.isProviderEnabled(LocationManager.GPS_PROVIDER)) {
                    locationManager.requestLocationUpdates(LocationManager.GPS_PROVIDER, 1000L, 0f, listener, looper);
                    requested = true;
                }
            } catch (SecurityException ignored) {}

            // Đặt timeout 3.5 giây: nếu GPS chưa khóa vệ tinh thì trả về fallbackLocation hoặc reject để Vue dùng IP fallback
            new Handler(Looper.getMainLooper()).postDelayed(() -> {
                if (resolved[0]) return;
                resolved[0] = true;
                try {
                    locationManager.removeUpdates(listener);
                } catch (Exception ignored) {}

                if (fallbackLocation != null) {
                    JSObject ret = new JSObject();
                    ret.put("latitude", fallbackLocation.getLatitude());
                    ret.put("longitude", fallbackLocation.getLongitude());
                    ret.put("accuracy", fallbackLocation.getAccuracy());
                    call.resolve(ret);
                } else {
                    call.reject("Hết thời gian chờ GPS, chuyển sang IP fallback.");
                }
            }, 3500);

            if (!requested && fallbackLocation != null) {
                resolved[0] = true;
                JSObject ret = new JSObject();
                ret.put("latitude", fallbackLocation.getLatitude());
                ret.put("longitude", fallbackLocation.getLongitude());
                ret.put("accuracy", fallbackLocation.getAccuracy());
                call.resolve(ret);
            }
        } catch (Exception e) {
            call.reject("Lỗi định vị: " + e.getMessage());
        }
    }

    @com.getcapacitor.annotation.PermissionCallback
    private void locationPermissionCallback(PluginCall call) {
        Context context = getContext();
        if (context == null) {
            call.reject("Context không khả dụng");
            return;
        }
        boolean hasFine = ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED;
        boolean hasCoarse = ActivityCompat.checkSelfPermission(context, Manifest.permission.ACCESS_COARSE_LOCATION) == PackageManager.PERMISSION_GRANTED;

        if (hasFine || hasCoarse) {
            obtainLocation(call);
        } else {
            call.reject("Người dùng đã từ chối quyền truy cập vị trí.");
        }
    }
}
