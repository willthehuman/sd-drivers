#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")] // hide console window on Windows in release
extern crate hidapi;
extern crate eframe;
extern crate keymapper;

use std::error::Error;
use eframe::egui;
use hidapi::HidApi;
use tokio::task::JoinHandle;
use keymapper::*;
use std::future::IntoFuture;

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
    hidapi: HidApi,
    is_driver_running: bool,
    driver_handle: Option<JoinHandle<()>>,
}

impl MyApp {
    fn new() -> Self {
        Self {
            hidapi: HidApi::new().unwrap(),
            is_driver_running: false,
            driver_handle: None,
        }
    }
}

impl eframe::App for MyApp {

    fn update(&mut self, ctx: &egui::Context, _frame: &mut eframe::Frame) {
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

