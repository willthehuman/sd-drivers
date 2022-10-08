#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")] // hide console window on Windows in release
extern crate eframe;
extern crate keymapper;
extern crate rusb;

use std::error::Error;
use eframe::egui;
use tokio::task::JoinHandle;
use keymapper::*;
use std::future::IntoFuture;
use rusb::{Device, DeviceHandle, GlobalContext, InterfaceDescriptors, UsbContext};
use rusb::ffi::{libusb_claim_interface, libusb_handle_events, libusb_ss_usb_device_capability_descriptor};

#[tokio::main]
async fn main() {
    let options = eframe::NativeOptions::default();
    eframe::run_native(
        "My egui App",
        options,
        Box::new(|_cc| Box::new(MyApp::new())),
    );
}

struct MyApp {
    neptune: DeviceHandle<GlobalContext>,
    is_driver_running: bool,
    driver_handle: Option<JoinHandle<()>>,
}

impl MyApp {
    fn new() -> Self {
        let mut neptune = rusb::open_device_with_vid_pid(0x28de, 0x1205).unwrap();
        let duration = std::time::Duration::from_millis(1000);
        let language = neptune.read_languages(duration).unwrap()[0];
        let res = neptune.claim_interface(0x84);
        println!("claim interface result: {:?}", res);

        for i in 0..=u8::MAX {
            let res = neptune.claim_interface(i);
            if let Ok(desc) = neptune.read_string_descriptor(language, i, duration) {
                println!("claim interface {:?} result: {:?}", res, desc);
            }
        }

        Self {
            neptune,
            is_driver_running: false,
            driver_handle: None,
        }
    }
}

impl eframe::App for MyApp {

    fn update(&mut self, ctx: &egui::Context, _frame: &mut eframe::Frame) {

        let mut rbuf = [0u8; 64];
        for i in 0..=u8::MAX {
            //clear terminal
            print!("{esc}c", esc = 27 as char);
            let _ = self.neptune.read_interrupt(i, &mut rbuf, std::time::Duration::from_millis(500));
            println!("read interrupt {:?} result: {:?}", i, rbuf);
        }


        println!("{:x?}", rbuf);

        egui::CentralPanel::default().show(ctx, |ui| {

            ui.heading("My egui Application");
            let mut toggle_button = ui.button((if self.is_driver_running {"Stop"} else {"Start"}).to_string());

            if toggle_button.clicked() {
                if self.is_driver_running {
                    // kill the driver
                    if let Some(driver_handle) = self.driver_handle.take() {
                        driver_handle.abort();
                        println!("Driver stopped");
                        self.is_driver_running = false;
                    }

                } else {
                    let handle = tokio::spawn(async {
                        let v = async { keymapper::main() };
                        let mut fut = v.into_future();
                        fut.await;
                    });

                    self.driver_handle = Some(handle);
                    self.is_driver_running = true;
                    println!("Driver started");
                }
            }
        });
    }
}

